# Документация: папка `terragrunt/`

Папка `terragrunt/` содержит **конфигурации окружений** для Terragrunt — инструмента, который оркестрирует вызовы Terraform-модулей для разных окружений (dev, prod, local).

---

## Что такое Terragrunt и зачем он нужен?

### Проблема без Terragrunt

Без Terragrunt для каждого окружения (dev, prod, local) и каждого модуля пришлось бы дублировать:

```hcl
# Это нужно было бы писать в КАЖДОМ из 6+ модулей:
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-tfstate"
    storage_account_name = "sttfsecurityrule"
    container_name       = "tfstate"
    key                  = "dev/k8s-cluster/terraform.tfstate"  # менять руками
  }
}

provider "azurerm" {
  features {}
  subscription_id = "..."  # дублировать везде
}
```

Это нарушает принцип DRY (Don't Repeat Yourself) и легко приводит к ошибкам.

### Решение с Terragrunt

Terragrunt:
- **генерирует** `backend.tf` и `provider.tf` для каждого модуля **автоматически**
- **управляет зависимостями** — запускает `k8s-cluster` до `k8s-app`
- **параметризует** окружения — dev/prod/local с разными значениями
- **запускает несколько модулей** одной командой: `terragrunt run-all apply`

---

## Структура папки

```
terragrunt/
├── terragrunt.hcl              ← КОРНЕВОЙ КОНФИГ
│                                  (backend + providers для всех дочерних)
│
├── _envcommon/                 ← ОБЩИЕ КОНФИГИ (переиспользуются в dev, prod)
│   ├── k8s-cluster.hcl         ← общие inputs для модуля k8s-cluster
│   └── k8s-app.hcl             ← общие inputs для модуля k8s-app
│
└── environments/               ← ОКРУЖЕНИЯ
    ├── dev/                    ← Разработка (дешевле, 1 нода)
    │   ├── env.hcl             ← environment=dev, region=westeurope
    │   ├── k8s-cluster/
    │   │   └── terragrunt.hcl  ← 1 нода, Standard_B2s
    │   └── k8s-app/
    │       └── terragrunt.hcl  ← 1 реплика приложения
    │
    ├── prod/                   ← Продакшн (надёжнее, 3 ноды)
    │   ├── env.hcl             ← environment=prod
    │   ├── k8s-cluster/
    │   │   └── terragrunt.hcl  ← 3 ноды, Standard_D4s_v3
    │   └── k8s-app/
    │       └── terragrunt.hcl  ← 2 реплики, стабильный образ
    │
    └── local/                  ← Локальная разработка (без облака)
        ├── env.hcl             ← environment=local
        └── k8s-local/
            └── terragrunt.hcl  ← kind кластер + переопределение backend
```

---

## `terragrunt/terragrunt.hcl` — Корневой конфиг

Этот файл находится в корне `terragrunt/` и автоматически «наследуется» всеми дочерними `terragrunt.hcl` файлами.

```hcl
# ─────────────────────────────────────────────
# locals — вычисляемые локальные переменные.
# Здесь читаем env.hcl из текущего окружения,
# чтобы знать имя окружения (dev, prod, local).
# ─────────────────────────────────────────────
locals {
  # Ищем env.hcl вверх по дереву директорий
  # path_relative_to_include() вернёт, например:
  # "environments/dev/k8s-cluster"
  env_vars = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  
  environment = local.env_vars.locals.environment   # "dev", "prod", "local"
  location    = local.env_vars.locals.location       # "westeurope"
}

# ─────────────────────────────────────────────
# remote_state — где хранится Terraform state.
#
# State — это файл, в котором Terraform
# записывает что уже создано. Хранить его
# в Azure Blob (а не локально) нужно для:
# - работы нескольких разработчиков без конфликтов
# - предотвращения потери state при переустановке
# - поддержки блокировок (state locking)
#
# Структура ключей: environment/module/terraform.tfstate
# Например: dev/k8s-cluster/terraform.tfstate
# ─────────────────────────────────────────────
remote_state {
  backend = "azurerm"  # Azure Blob Storage

  generate = {
    path      = "backend.tf"   # имя генерируемого файла
    if_exists = "overwrite"    # перезаписывать при каждом init
  }

  config = {
    # Storage Account для хранения state.
    # Создайте один раз вручную (или через bootstrap скрипт).
    resource_group_name  = "rg-tfstate-securityrule"
    storage_account_name = "sttfsecurityrule"
    container_name       = "tfstate"
    
    # Уникальный ключ для каждого модуля каждого окружения.
    # path_relative_to_include() = "environments/dev/k8s-cluster"
    key = "${local.environment}/${path_relative_to_include()}/terraform.tfstate"
    
    # Блокировка state во время apply (предотвращает параллельные apply)
    use_azuread_auth = true
  }
}

# ─────────────────────────────────────────────
# generate — генерирует файл в директории модуля
# перед запуском terraform.
#
# Здесь генерируем provider.tf с настройками Azure.
# Без этого пришлось бы дублировать provider в каждом модуле.
# ─────────────────────────────────────────────
generate "provider_azurerm" {
  path      = "provider_azurerm.tf"
  if_exists = "overwrite_terragrunt"

  contents = <<EOF
# Этот файл автоматически сгенерирован Terragrunt.
# Не редактируйте вручную — изменения будут перезаписаны.
provider "azurerm" {
  features {}
  # subscription_id берётся из переменной окружения ARM_SUBSCRIPTION_ID
  # или из az login (Azure CLI)
}
EOF
}

# ─────────────────────────────────────────────
# inputs — переменные, которые Terragrunt
# передаёт в Terraform как TF_VAR_*
# Здесь — теги для всех Azure ресурсов
# ─────────────────────────────────────────────
inputs = {
  tags = {
    environment = local.environment
    project     = "security-rule"
    managed_by  = "terragrunt"
  }
}
```

---

## `terragrunt/_envcommon/k8s-cluster.hcl` — Общий конфиг кластера

Содержит inputs, **одинаковые для всех окружений**. Конкретные окружения включают этот файл и могут переопределить нужные значения.

```hcl
# ─────────────────────────────────────────────
# Читаем env.hcl родительского окружения,
# чтобы получить доступ к local.environment и local.location
# ─────────────────────────────────────────────
locals {
  env_vars    = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  environment = local.env_vars.locals.environment
  location    = local.env_vars.locals.location
}

# ─────────────────────────────────────────────
# Указываем Terragrunt, какой Terraform модуль
# нужно использовать. Путь относительный — от
# корня репозитория.
# ─────────────────────────────────────────────
terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-cluster"
  # Двойной слэш // — разделитель: слева путь к модулю,
  # справа (пустой) — путь внутри модуля
}

inputs = {
  # Имена ресурсов включают имя окружения, чтобы dev и prod
  # не пересекались в одной подписке Azure.
  # dev  → rg-securityrule-dev
  # prod → rg-securityrule-prod
  resource_group_name = "rg-securityrule-${local.environment}"
  location            = local.location
  
  # aks-securityrule-dev или aks-securityrule-prod
  cluster_name = "aks-securityrule-${local.environment}"
}
```

---

## `terragrunt/_envcommon/k8s-app.hcl` — Общий конфиг приложения

```hcl
locals {
  env_vars    = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  environment = local.env_vars.locals.environment
}

terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-app"
}

inputs = {
  namespace = "security-rule"
  
  # В prod используем Production (скрывает подробные ошибки),
  # в dev — Development (подробные стектрейсы в браузере)
  environment = local.environment == "prod" ? "Production" : "Development"
  
  # Образ приложения из GitHub Container Registry
  app_image = "ghcr.io/ermak-p/securityrule:latest"
}
```

---

## `terragrunt/environments/dev/env.hcl` — Переменные dev-окружения

```hcl
# ─────────────────────────────────────────────
# env.hcl — файл с переменными конкретного окружения.
# Читается корневым terragrunt.hcl и _envcommon/*.hcl
# через read_terragrunt_config().
# ─────────────────────────────────────────────
locals {
  # Имя окружения — используется в именах ресурсов
  # и для выбора конфигурации
  environment = "dev"
  
  # Регион Azure
  location = "westeurope"
}
```

---

## `terragrunt/environments/dev/k8s-cluster/terragrunt.hcl` — Dev кластер

```hcl
# ─────────────────────────────────────────────
# include — включаем родительские конфиги.
# "root" — корневой terragrunt.hcl (backend + providers)
# "envcommon" — общий конфиг k8s-cluster
# ─────────────────────────────────────────────
include "root" {
  path = find_in_parent_folders()  # находит terragrunt/terragrunt.hcl
}

include "envcommon" {
  path   = "${dirname(find_in_parent_folders())}/_envcommon/k8s-cluster.hcl"
  expose = true  # делает locals из envcommon доступными здесь
}

# ─────────────────────────────────────────────
# inputs — переопределяем только то, что отличается
# в dev от общего конфига.
#
# Dev: 1 нода, Standard_B2s (дешевле)
# - Standard_B2s: 2 vCPU, 4 GB RAM (~$30/месяц)
# - Нет multi-az отказоустойчивости
# ─────────────────────────────────────────────
inputs = {
  node_count = 1
  vm_size    = "Standard_B2s"
}
```

---

## `terragrunt/environments/dev/k8s-app/terragrunt.hcl` — Dev приложение

```hcl
include "root" {
  path = find_in_parent_folders()
}

include "envcommon" {
  path   = "${dirname(find_in_parent_folders())}/_envcommon/k8s-app.hcl"
  expose = true
}

# ─────────────────────────────────────────────
# dependency — объявляем зависимость от модуля k8s-cluster.
# Terragrunt автоматически:
# 1. Запустит k8s-cluster до k8s-app (при run-all apply)
# 2. Прочитает outputs k8s-cluster после его применения
# 3. Передаст outputs как переменные в k8s-app
#
# mock_outputs используются при terraform plan — когда
# реального кластера ещё нет, plan не должен падать.
# ─────────────────────────────────────────────
dependency "cluster" {
  config_path = "../k8s-cluster"
  
  mock_outputs = {
    host                   = "https://mock-host"
    client_certificate     = "bW9jaw=="    # base64("mock")
    client_key             = "bW9jaw=="
    cluster_ca_certificate = "bW9jaw=="
  }
  
  # Если кластер не применён — использовать mock (для plan)
  mock_outputs_allowed_terraform_commands = ["plan", "validate"]
}

# ─────────────────────────────────────────────
# Генерируем provider_kubernetes.tf с реквизитами кластера.
# Используем outputs из модуля k8s-cluster.
# ─────────────────────────────────────────────
generate "provider_kubernetes" {
  path      = "provider_kubernetes.tf"
  if_exists = "overwrite_terragrunt"
  
  contents = <<EOF
provider "kubernetes" {
  host = "${dependency.cluster.outputs.host}"
  
  client_certificate     = base64decode("${dependency.cluster.outputs.client_certificate}")
  client_key             = base64decode("${dependency.cluster.outputs.client_key}")
  cluster_ca_certificate = base64decode("${dependency.cluster.outputs.cluster_ca_certificate}")
}
EOF
}

inputs = {
  # Реквизиты кластера (из dependency outputs)
  kube_host                   = dependency.cluster.outputs.host
  kube_client_certificate     = dependency.cluster.outputs.client_certificate
  kube_client_key             = dependency.cluster.outputs.client_key
  kube_cluster_ca_certificate = dependency.cluster.outputs.cluster_ca_certificate

  # Dev-специфичные настройки
  app_replicas     = 1
  development_user = "dev-user"

  # Секреты — должны передаваться через переменные окружения
  # (никогда не хардкодить в .tf файлах!):
  # export TF_VAR_db_connection_string="Server=..."
  # export TF_VAR_fakead_connection_string="Server=..."
  # export TF_VAR_mssql_sa_password="..."
}
```

---

## `terragrunt/environments/prod/k8s-cluster/terragrunt.hcl` — Prod кластер

```hcl
include "root" {
  path = find_in_parent_folders()
}

include "envcommon" {
  path   = "${dirname(find_in_parent_folders())}/_envcommon/k8s-cluster.hcl"
  expose = true
}

# ─────────────────────────────────────────────
# Prod: 3 ноды, более мощные VM
#
# Standard_D4s_v3: 4 vCPU, 16 GB RAM (~$140/месяц × 3 = ~$420)
# 3 ноды = нет single point of failure:
# - если одна нода упала, поды переезжают на другую
# - обновление нод без downtime (rolling)
# ─────────────────────────────────────────────
inputs = {
  node_count = 3
  vm_size    = "Standard_D4s_v3"
}
```

---

## `terragrunt/environments/prod/k8s-app/terragrunt.hcl` — Prod приложение

```hcl
include "root" { path = find_in_parent_folders() }

include "envcommon" {
  path   = "${dirname(find_in_parent_folders())}/_envcommon/k8s-app.hcl"
  expose = true
}

dependency "cluster" {
  config_path = "../k8s-cluster"
  mock_outputs = {
    host = "https://mock-host"
    client_certificate = "bW9jaw=="; client_key = "bW9jaw=="
    cluster_ca_certificate = "bW9jaw=="
  }
  mock_outputs_allowed_terraform_commands = ["plan", "validate"]
}

generate "provider_kubernetes" {
  path      = "provider_kubernetes.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<EOF
provider "kubernetes" {
  host                   = "${dependency.cluster.outputs.host}"
  client_certificate     = base64decode("${dependency.cluster.outputs.client_certificate}")
  client_key             = base64decode("${dependency.cluster.outputs.client_key}")
  cluster_ca_certificate = base64decode("${dependency.cluster.outputs.cluster_ca_certificate}")
}
EOF
}

inputs = {
  kube_host                   = dependency.cluster.outputs.host
  kube_client_certificate     = dependency.cluster.outputs.client_certificate
  kube_client_key             = dependency.cluster.outputs.client_key
  kube_cluster_ca_certificate = dependency.cluster.outputs.cluster_ca_certificate

  # 2 реплики для отказоустойчивости (важно с sticky sessions в Ingress)
  app_replicas = 2

  # Стабильный тег вместо :latest (предсказуемые деплои)
  app_image = "ghcr.io/ermak-p/securityrule:stable"
}
```

---

## `terragrunt/environments/local/env.hcl`

```hcl
locals {
  environment = "local"
  location    = "local"  # не используется (нет Azure)
}
```

---

## `terragrunt/environments/local/k8s-local/terragrunt.hcl` — Локальное окружение

Самый особенный конфиг — переопределяет backend и provider, т.к. нет Azure.

```hcl
# ─────────────────────────────────────────────
# Для локального окружения переопределяем backend:
# вместо Azure Blob Storage используем локальный файл.
# Так не нужен Azure-аккаунт для работы локально.
# ─────────────────────────────────────────────
remote_state {
  backend = "local"  # хранить state локально
  
  generate = {
    path      = "backend.tf"
    if_exists = "overwrite"
  }
  
  config = {
    path = "${get_repo_root()}/.terraform-local-state/local/terraform.tfstate"
  }
}

# ─────────────────────────────────────────────
# Генерируем provider_kubernetes.tf для kind.
# В отличие от облака, используем kubeconfig файл
# (создаётся автоматически при kind create cluster).
# ─────────────────────────────────────────────
generate "provider_kubernetes" {
  path      = "provider_kubernetes.tf"
  if_exists = "overwrite_terragrunt"

  contents = <<EOF
provider "kubernetes" {
  config_path    = "~/.kube/config"
  config_context = "kind-security-rule-local"  # имя контекста в kubeconfig
}
EOF
}

# ─────────────────────────────────────────────
# Указываем Terraform модуль для локального окружения.
# Это другой модуль (k8s-local), не k8s-app.
# ─────────────────────────────────────────────
terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-local"
}

# ─────────────────────────────────────────────
# Настройки локального окружения.
# Пароль можно переопределить через переменную окружения:
# export TF_VAR_mssql_sa_password="MyStrongPass!123"
# ─────────────────────────────────────────────
inputs = {
  cluster_name          = "security-rule-local"
  namespace             = "security-rule"
  app_image             = "security-rule:local"
  app_image_pull_policy = "Never"  # локальный образ, не качать из registry
  mssql_sa_password     = "LocalDev!Passw0rd"
  development_user      = "local-dev-user"
  host_port_app         = 8080
  host_port_mssql       = 1433
}
```

---

## Terragrunt: команды

### Работа с одним окружением

```bash
# Перейти в нужное окружение
cd terragrunt/environments/local/k8s-local

# Инициализация (один раз или при смене бэкенда)
terragrunt init

# Посмотреть план изменений
terragrunt plan

# Применить изменения
terragrunt apply

# Удалить все ресурсы
terragrunt destroy

# Принудительно пересоздать ресурс
terragrunt apply -replace kubernetes_deployment.app
```

### Работа со всем окружением (run-all)

```bash
# Применить все модули dev окружения в правильном порядке
cd terragrunt/environments/dev
terragrunt run-all apply

# Удалить всё dev окружение
terragrunt run-all destroy

# Посмотреть план для всех модулей
terragrunt run-all plan
```

`run-all` автоматически определяет порядок на основе `dependency` блоков:
1. Сначала `k8s-cluster` (нет зависимостей)
2. Потом `k8s-app` (зависит от `k8s-cluster`)

### Передача секретов

Никогда не хардкодьте пароли в `.hcl` файлах! Используйте переменные окружения:

```bash
# Вариант 1: переменные окружения
export TF_VAR_mssql_sa_password="MySecurePass!123"
export TF_VAR_db_connection_string="Server=...;Password=MySecurePass!123;..."
export TF_VAR_fakead_connection_string="Server=...;Password=MySecurePass!123;..."
terragrunt apply

# Вариант 2: файл переменных (не добавлять в git!)
cat > /tmp/secrets.tfvars <<'EOF'
mssql_sa_password        = "MySecurePass!123"
db_connection_string     = "Server=...;"
fakead_connection_string = "Server=...;"
EOF
TF_CLI_ARGS_apply="-var-file=/tmp/secrets.tfvars" terragrunt apply

# Вариант 3: Azure Key Vault (prod, рекомендуется)
# Интеграция через External Secrets Operator или Vault Agent
```

---

## Поток наследования конфигов

Когда вы запускаете `terragrunt apply` в `environments/dev/k8s-app/`:

```
environments/dev/k8s-app/terragrunt.hcl
    │
    ├── include "root" → terragrunt/terragrunt.hcl
    │       ├── remote_state → генерирует backend.tf
    │       ├── generate "provider_azurerm" → генерирует provider_azurerm.tf
    │       └── inputs (tags) → передаются в Terraform
    │
    ├── include "envcommon" → _envcommon/k8s-app.hcl
    │       ├── terraform.source → путь к terraform/modules/k8s-app
    │       └── inputs (namespace, environment, app_image) → Terraform vars
    │
    ├── dependency "cluster" → читает outputs из environments/dev/k8s-cluster/
    │
    ├── generate "provider_kubernetes" → генерирует provider_kubernetes.tf
    │
    └── inputs (app_replicas, kube_host, ...) → Terraform vars

    ↓ Всё это объединяется и передаётся в:
    
terraform/modules/k8s-app/ с переменными:
    - namespace = "security-rule"
    - environment = "Development"
    - app_image = "ghcr.io/ermak-p/securityrule:latest"
    - app_replicas = 1
    - kube_host = "https://..."  (из dependency)
    - tags = {environment="dev", ...}  (из root)
```

---

## Разница между окружениями

| Параметр | local | dev | prod |
|---|---|---|---|
| Где запускается | Docker (kind) | Azure AKS | Azure AKS |
| Кол-во нод | 1 (kind) | 1 | 3 |
| Тип VM | N/A | Standard_B2s | Standard_D4s_v3 |
| Реплики приложения | 1 | 1 | 2 |
| Backend state | локальный файл | Azure Blob | Azure Blob |
| Kubernetes provider | kubeconfig файл | сертификаты AKS | сертификаты AKS |
| Образ приложения | `security-rule:local` | `:latest` | `:stable` |
| Нужен Azure | ❌ | ✅ | ✅ |

---

## Файлы которые НЕ нужно коммитить в git

Добавьте в `.gitignore`:

```gitignore
# Terraform state и кэш
**/.terraform/
**/.terraform.lock.hcl
**/terraform.tfstate
**/terraform.tfstate.backup
.terraform-local-state/

# Сгенерированные файлы Terragrunt
**/backend.tf
**/provider_azurerm.tf
**/provider_kubernetes.tf

# Файлы с секретами
*.tfvars
*.tfvars.json
!*.example.tfvars  # примеры - можно в git
```

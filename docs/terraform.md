# Документация: папка `terraform/`

Папка `terraform/` содержит **переиспользуемые Terraform-модули** для развёртывания Kubernetes кластера и приложения SecurityRule. Модули написаны на языке HCL (HashiCorp Configuration Language).

---

## Что такое Terraform?

**Terraform** — инструмент «Infrastructure as Code» (IaC) от HashiCorp.

**Принцип работы:**
1. Вы описываете инфраструктуру в `.tf`-файлах: «мне нужен AKS кластер с такими параметрами»
2. Terraform сравнивает описание с реальностью (через **state**)
3. Terraform вычисляет и применяет минимальный набор изменений

**Ключевые команды:**
```bash
terraform init    # скачать провайдеры (.terraform/)
terraform plan    # показать что изменится (не применяя)
terraform apply   # применить изменения
terraform destroy # удалить все ресурсы
```

**State (состояние)** — JSON-файл, где Terraform записывает, какие ресурсы он создал и их атрибуты. Без state Terraform не может знать, что уже существует. В продакшене state хранится удалённо (Azure Blob, S3).

---

## Структура папки

```
terraform/
└── modules/
    ├── k8s-cluster/    ← Модуль 1: создаёт облачный кластер AKS (Azure)
    │   ├── main.tf     ← ресурсы Azure
    │   ├── variables.tf← входные параметры модуля
    │   └── outputs.tf  ← выходные значения (для других модулей)
    │
    ├── k8s-app/        ← Модуль 2: деплоит приложение в готовый кластер
    │   ├── main.tf     ← Kubernetes ресурсы (Deployment, Service и т.д.)
    │   ├── variables.tf← входные параметры
    │   └── outputs.tf  ← выходные значения
    │
    └── k8s-local/      ← Модуль 3: создаёт локальный кластер через kind
        ├── main.tf     ← kind кластер + SQL Server + приложение
        ├── variables.tf← настройки (порты, пароль, образ)
        └── outputs.tf  ← URL приложения, kubeconfig
```

**Модули** — это «строительные блоки». Каждый модуль можно вызвать из нескольких мест с разными параметрами. Terragrunt вызывает эти модули для разных окружений.

---

## Модуль `terraform/modules/k8s-cluster/`

### Назначение

Создаёт **AKS (Azure Kubernetes Service)** кластер в облаке Azure. Используется для dev и prod окружений, когда нужен настоящий облачный Kubernetes.

### `main.tf`

```hcl
# ─────────────────────────────────────────────
# Группа ресурсов Azure — логический контейнер
# для всех ресурсов одного проекта/окружения.
# Все ресурсы Azure должны принадлежать группе.
# ─────────────────────────────────────────────
resource "azurerm_resource_group" "this" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

# ─────────────────────────────────────────────
# AKS кластер — управляемый Kubernetes в Azure.
# Azure берёт на себя управление control plane
# (API server, etcd, scheduler), вам нужно
# только управлять нодами (worker nodes).
# ─────────────────────────────────────────────
resource "azurerm_kubernetes_cluster" "this" {
  name                = var.cluster_name
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  dns_prefix          = var.cluster_name

  # Пул нод по умолчанию (обязательный)
  default_node_pool {
    name       = "default"
    node_count = var.node_count  # кол-во виртуальных машин
    vm_size    = var.vm_size     # тип VM (CPU, RAM)
  }

  # Системное назначенное managed identity — Azure сам создаёт
  # сервисный аккаунт и управляет его ключами. Нам не нужно
  # хранить client_id и client_secret вручную.
  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}
```

#### Что такое AKS?

AKS (Azure Kubernetes Service) — управляемый Kubernetes:
- Azure управляет **control plane** (мастер-нодами, etcd, API server) — бесплатно
- Вы платите только за **worker nodes** (виртуальные машины с вашими подами)
- Автообновление, мониторинг, интеграция с Azure AD — из коробки

#### Managed Identity vs Service Principal

```hcl
identity {
  type = "SystemAssigned"  # предпочтительно
}
# Альтернатива (устаревший способ):
# service_principal {
#   client_id     = "..."
#   client_secret = "..."  # нужно хранить и ротировать вручную
# }
```

`SystemAssigned` — Azure автоматически создаёт identity для кластера и управляет ротацией ключей. Намного безопаснее, чем хранить секреты вручную.

---

### `variables.tf`

```hcl
# ─────────────────────────────────────────────
# Имя группы ресурсов Azure.
# Пример: rg-securityrule-dev, rg-securityrule-prod
# ─────────────────────────────────────────────
variable "resource_group_name" {
  description = "Name of the Azure Resource Group to create"
  type        = string
}

# ─────────────────────────────────────────────
# Регион Azure для размещения ресурсов.
# Список регионов: az account list-locations -o table
# Рекомендуется выбирать ближайший к пользователям.
# ─────────────────────────────────────────────
variable "location" {
  description = "Azure region (e.g., 'westeurope', 'eastus')"
  type        = string
  default     = "westeurope"
}

# ─────────────────────────────────────────────
# Имя AKS кластера.
# Пример: aks-securityrule-dev
# ─────────────────────────────────────────────
variable "cluster_name" {
  description = "Name of the AKS cluster"
  type        = string
}

# ─────────────────────────────────────────────
# Количество нод (виртуальных машин) в кластере.
# - dev: 1 нода (дешевле, но нет отказоустойчивости)
# - prod: 3 ноды (рекомендуется для production)
# Каждая нода запускает несколько подов.
# ─────────────────────────────────────────────
variable "node_count" {
  description = "Number of worker nodes in the node pool"
  type        = number
  default     = 1
}

# ─────────────────────────────────────────────
# Тип виртуальной машины для нод кластера.
# Standard_B2s  = 2 vCPU, 4 GB RAM  (dev, дешевле)
# Standard_D2s_v3 = 2 vCPU, 8 GB RAM (prod)
# Standard_D4s_v3 = 4 vCPU, 16 GB RAM (prod нагрузка)
# Полный список: az vm list-sizes --location westeurope -o table
# ─────────────────────────────────────────────
variable "vm_size" {
  description = "Azure VM size for cluster nodes"
  type        = string
  default     = "Standard_D2s_v3"
}

# ─────────────────────────────────────────────
# Теги — метаданные ресурсов Azure.
# Используются для: биллинга, фильтрации, отчётов.
# Рекомендуемые теги: environment, project, managed_by.
# ─────────────────────────────────────────────
variable "tags" {
  description = "Tags to apply to all Azure resources"
  type        = map(string)
  default     = {}
}
```

---

### `outputs.tf`

```hcl
# ─────────────────────────────────────────────
# Outputs — значения, которые модуль «возвращает»
# после применения. Используются модулем k8s-app
# для подключения к созданному кластеру.
# ─────────────────────────────────────────────

# URL Kubernetes API сервера.
# Пример: https://aks-securityrule-dev-abc123.hcp.westeurope.azmk8s.io:443
output "host" {
  description = "AKS API server endpoint URL"
  value       = azurerm_kubernetes_cluster.this.kube_config.0.host
  sensitive   = true
}

# Клиентский сертификат для аутентификации в Kubernetes API.
# Вместе с client_key составляет keypair для mTLS.
output "client_certificate" {
  description = "Base64-encoded client certificate for Kubernetes authentication"
  value       = azurerm_kubernetes_cluster.this.kube_config.0.client_certificate
  sensitive   = true
}

# Приватный ключ клиентского сертификата.
output "client_key" {
  description = "Base64-encoded client private key"
  value       = azurerm_kubernetes_cluster.this.kube_config.0.client_key
  sensitive   = true
}

# CA-сертификат кластера для верификации TLS.
# Без него нельзя проверить подлинность API сервера.
output "cluster_ca_certificate" {
  description = "Base64-encoded cluster CA certificate"
  value       = azurerm_kubernetes_cluster.this.kube_config.0.cluster_ca_certificate
  sensitive   = true
}
```

Эти outputs используются в `k8s-app` для настройки Kubernetes provider:
```hcl
provider "kubernetes" {
  host                   = dependency.cluster.outputs.host
  client_certificate     = base64decode(dependency.cluster.outputs.client_certificate)
  client_key             = base64decode(dependency.cluster.outputs.client_key)
  cluster_ca_certificate = base64decode(dependency.cluster.outputs.cluster_ca_certificate)
}
```

---

## Модуль `terraform/modules/k8s-app/`

### Назначение

Деплоит приложение SecurityRule и SQL Server в **уже существующий** Kubernetes кластер (созданный модулем `k8s-cluster` или существующий AKS). Создаёт те же ресурсы, что описаны в `k8s/base/`, но через Terraform provider (не YAML).

**Преимущество перед `kubectl apply`:** секреты (пароли) передаются как Terraform-переменные с пометкой `sensitive = true` и никогда не попадают в git.

### `main.tf`

```hcl
# ─────────────────────────────────────────────
# Namespace — логическая изоляция всех ресурсов
# приложения внутри кластера.
# ─────────────────────────────────────────────
resource "kubernetes_namespace" "this" {
  metadata {
    name = var.namespace
  }
}

# ─────────────────────────────────────────────
# ConfigMap — несекретные настройки приложения.
# Переменные попадают в контейнер как env vars.
# ─────────────────────────────────────────────
resource "kubernetes_config_map" "app" {
  metadata {
    name      = "app-config"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    ASPNETCORE_ENVIRONMENT               = var.environment
    ASPNETCORE_URLS                      = "http://+:8080"
    Authentication__UseActiveDirectory   = "false"
    Authentication__DevelopmentUser      = var.development_user
    Logging__LogLevel__Default           = "Information"
    "Logging__LogLevel__Microsoft.AspNetCore" = "Warning"
  }
}

# ─────────────────────────────────────────────
# Secret — строки подключения к БД.
# sensitive = true в переменных гарантирует,
# что значения не появятся в terraform output
# и terraform plan в открытом виде.
# ─────────────────────────────────────────────
resource "kubernetes_secret" "app" {
  metadata {
    name      = "app-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    ConnectionStrings__DefaultConnection = var.db_connection_string
    ConnectionStrings__FakeAdConnection  = var.fakead_connection_string
  }
}

# ─────────────────────────────────────────────
# Secret — пароль SA для SQL Server.
# Отдельный Secret, т.к. используется только
# StatefulSet SQL Server, не приложением.
# ─────────────────────────────────────────────
resource "kubernetes_secret" "mssql" {
  metadata {
    name      = "mssql-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    sa-password = var.mssql_sa_password
  }
}

# ─────────────────────────────────────────────
# Deployment приложения SecurityRule.
# Управляет запуском подов с Blazor-приложением.
# ─────────────────────────────────────────────
resource "kubernetes_deployment" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    replicas = var.app_replicas

    selector {
      match_labels = { app = "security-rule" }
    }

    template {
      metadata {
        labels = { app = "security-rule" }
      }

      spec {
        container {
          name  = "security-rule"
          image = var.app_image

          port { container_port = 8080 }

          # Подключаем все ключи ConfigMap как env vars
          env_from {
            config_map_ref { name = kubernetes_config_map.app.metadata[0].name }
          }
          # Подключаем все ключи Secret как env vars
          env_from {
            secret_ref { name = kubernetes_secret.app.metadata[0].name }
          }

          # Проверка: жив ли под? Если нет — перезапустить.
          liveness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 30
            period_seconds        = 15
            failure_threshold     = 3
          }

          # Проверка: готов ли принимать трафик?
          readiness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 15
            period_seconds        = 10
            failure_threshold     = 3
          }

          resources {
            requests = { memory = "256Mi", cpu = "100m" }
            limits   = { memory = "512Mi", cpu = "500m" }
          }
        }
      }
    }
  }
}

# ─────────────────────────────────────────────
# Service — стабильный DNS-адрес и балансировка
# для подов приложения. Тип ClusterIP = только
# внутри кластера (Ingress обеспечивает внешний доступ).
# ─────────────────────────────────────────────
resource "kubernetes_service" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    selector = { app = "security-rule" }
    port {
      port        = 80      # внешний порт сервиса
      target_port = 8080    # порт контейнера
    }
  }
}
```

### `variables.tf`

```hcl
# Параметры подключения к кластеру — передаются из модуля k8s-cluster
variable "kube_host"                   { type = string; sensitive = true }
variable "kube_client_certificate"     { type = string; sensitive = true }
variable "kube_client_key"             { type = string; sensitive = true }
variable "kube_cluster_ca_certificate" { type = string; sensitive = true }

# Настройки окружения
variable "namespace"        { type = string; default = "security-rule" }
variable "environment"      { type = string; default = "Production" }
variable "development_user" { type = string; default = "k8s-user" }

# Настройки приложения
variable "app_image"    { type = string } # Docker image с тегом
variable "app_replicas" { type = number; default = 1 }

# ─────────────────────────────────────────────
# sensitive = true — Terraform скрывает значение:
# - в выводе terraform plan (выводится как "(sensitive value)")
# - в выводе terraform output (требует --raw флага)
# - в state файле значение сохраняется, но помечается как sensitive
# ─────────────────────────────────────────────
variable "db_connection_string" {
  description = "SQL Server connection string for SecurityRuleDb"
  type        = string
  sensitive   = true
}

variable "fakead_connection_string" {
  description = "SQL Server connection string for FakeAdDb"
  type        = string
  sensitive   = true
}

variable "mssql_sa_password" {
  description = "SA password for SQL Server (min 8 chars, upper+lower+digit+special)"
  type        = string
  sensitive   = true
}
```

---

## Модуль `terraform/modules/k8s-local/`

### Назначение

Создаёт **полный локальный стек** для разработки:
- kind (Kubernetes IN Docker) кластер
- SQL Server 2022 в поде
- Приложение SecurityRule в поде
- Проброс портов: `localhost:8080` → приложение, `localhost:1433` → SQL Server

Не требует Azure-аккаунта, работает полностью на вашей машине через Docker.

### `main.tf` — разбор блоков

#### Блок 1: kind кластер

```hcl
# ─────────────────────────────────────────────
# kind (Kubernetes IN Docker) — запускает
# Kubernetes кластер в Docker-контейнерах.
# Каждая "нода" кластера = Docker контейнер.
#
# extra_port_mappings — проброс портов с хоста
# в кластер, чтобы можно было открыть приложение
# в браузере на localhost.
# ─────────────────────────────────────────────
resource "kind_cluster" "this" {
  name = var.cluster_name

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"

      # localhost:8080 → порт 8080 на ноде → Service NodePort → контейнер
      extra_port_mappings {
        container_port = 30080  # NodePort сервиса приложения
        host_port      = var.host_port_app
        protocol       = "TCP"
      }

      # localhost:1433 → порт 1433 на ноде → Service NodePort → SQL Server
      extra_port_mappings {
        container_port = 30433  # NodePort сервиса SQL Server
        host_port      = var.host_port_mssql
        protocol       = "TCP"
      }
    }
  }
}
```

**Почему NodePort 30080/30433?**

В kind нельзя использовать LoadBalancer (нет облака), поэтому Services типа NodePort. NodePort открывает порт в диапазоне 30000-32767 на каждой ноде кластера. Через `extra_port_mappings` этот порт ноды проброшен на хост.

#### Блок 2: Namespace

```hcl
resource "kubernetes_namespace" "this" {
  metadata { name = var.namespace }
  
  # Зависимость: namespace создаётся после кластера
  depends_on = [kind_cluster.this]
}
```

`depends_on` — явная зависимость. Terraform создаёт ресурсы параллельно, но здесь namespace **должен** создаваться только после кластера.

#### Блок 3: SQL Server StatefulSet

```hcl
resource "kubernetes_stateful_set" "mssql" {
  metadata {
    name      = "mssql"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    selector { match_labels = { app = "mssql" } }
    service_name = "mssql"
    replicas     = 1

    template {
      spec {
        container {
          name  = "mssql"
          image = "mcr.microsoft.com/mssql/server:2022-latest"

          env {
            name  = "ACCEPT_EULA"   # Обязательно — принятие лицензии SQL Server
            value = "Y"
          }
          env {
            name = "MSSQL_SA_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.mssql.metadata[0].name
                key  = "sa-password"  # берём из Secret
              }
            }
          }

          volume_mount {
            name       = "mssql-data"
            mount_path = "/var/opt/mssql"  # данные SQL Server
          }

          resources {
            requests = { memory = "2Gi", cpu = "500m" }
            limits   = { memory = "4Gi", cpu = "2" }
          }
        }

        volume {
          name = "mssql-data"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.mssql.metadata[0].name
          }
        }
      }
    }
  }
}
```

#### Блок 4: Приложение с init-контейнером

```hcl
resource "kubernetes_deployment" "app" {
  spec {
    template {
      spec {
        # ─────────────────────────────────────────────
        # Init-контейнер запускается ПЕРЕД основным
        # контейнером и должен успешно завершиться.
        # Здесь он ждёт, пока SQL Server примет соединения.
        #
        # Без init-контейнера приложение упадёт с ошибкой
        # подключения к БД и попадёт в CrashLoopBackOff.
        # ─────────────────────────────────────────────
        init_container {
          name    = "wait-for-mssql"
          image   = "busybox:1.36"
          command = [
            "sh", "-c",
            # nc (netcat) проверяет TCP-соединение на порту 1433
            # -z = только проверить, не передавать данные
            # || sleep 2 = при неудаче подождать 2 секунды
            "until nc -z mssql 1433; do echo 'waiting for mssql...'; sleep 2; done"
          ]
        }

        container {
          name  = "security-rule"
          image = var.app_image
          # imagePullPolicy: Never — не скачивать из registry,
          # использовать только образ загруженный через kind load
          image_pull_policy = var.app_image_pull_policy

          env_from {
            config_map_ref { name = kubernetes_config_map.app.metadata[0].name }
          }
          env_from {
            secret_ref { name = kubernetes_secret.app.metadata[0].name }
          }

          port { container_port = 8080 }
        }
      }
    }
  }
}
```

**Почему `imagePullPolicy: Never`?**

kind не имеет доступа к вашему локальному Docker registry. Чтобы использовать локально собранный образ, нужно:
1. Загрузить его в kind: `kind load docker-image security-rule:local --name security-rule-local`
2. Указать `imagePullPolicy: Never` — Kubernetes не будет пытаться скачать образ из registry

#### Блок 5: Services типа NodePort

```hcl
# NodePort открывает порт на каждой ноде кластера.
# В kind этот порт проброшен на localhost через extra_port_mappings.
resource "kubernetes_service" "app" {
  spec {
    selector = { app = "security-rule" }
    type     = "NodePort"
    port {
      port        = 80
      target_port = 8080
      node_port   = 30080  # должен совпадать с container_port в extra_port_mappings
    }
  }
}

resource "kubernetes_service" "mssql" {
  spec {
    selector = { app = "mssql" }
    type     = "NodePort"
    port {
      port        = 1433
      target_port = 1433
      node_port   = 30433
    }
  }
}
```

---

### `variables.tf`

```hcl
# Имя kind кластера — используется в kubeconfig
variable "cluster_name" {
  type    = string
  default = "security-rule-local"
}

variable "namespace" {
  type    = string
  default = "security-rule"
}

# Образ приложения — должен быть загружен через kind load
variable "app_image" {
  type    = string
  default = "security-rule:local"
}

# Never = не скачивать, использовать локальный образ
# IfNotPresent = скачать если нет локально (для registry)
variable "app_image_pull_policy" {
  type    = string
  default = "Never"
}

# Пароль SA для SQL Server. Требования к сложности:
# - минимум 8 символов
# - заглавные буквы
# - строчные буквы
# - цифры
# - специальные символы (!@#$%^&*)
variable "mssql_sa_password" {
  type      = string
  sensitive = true
  default   = "LocalDev!Passw0rd"
}

# Имя пользователя для DevelopmentAuthenticationHandler
variable "development_user" {
  type    = string
  default = "local-dev-user"
}

# Порт на хосте для приложения (localhost:8080)
variable "host_port_app" {
  type    = number
  default = 8080
}

# Порт на хосте для SQL Server (localhost:1433)
variable "host_port_mssql" {
  type    = number
  default = 1433
}
```

---

### `outputs.tf`

```hcl
# URL приложения в браузере
output "app_url" {
  description = "Application URL (open in browser after deploy)"
  value       = "http://localhost:${var.host_port_app}"
}

# Имя кластера (используется в kubectl и kind командах)
output "cluster_name" {
  description = "kind cluster name"
  value       = kind_cluster.this.name
}

# Путь к kubeconfig (обычно ~/.kube/config)
output "kubeconfig_path" {
  description = "Path to kubeconfig file"
  value       = kind_cluster.this.kubeconfig_path
}

# Строка подключения для SSMS/Rider/Azure Data Studio
output "mssql_connection" {
  description = "SQL Server connection string for IDE/SSMS"
  value       = "Server=localhost,${var.host_port_mssql};Database=SecurityRuleDb;User Id=sa;Password=${var.mssql_sa_password};TrustServerCertificate=True;"
  sensitive   = true
}
```

---

## Провайдеры Terraform

Каждый модуль использует провайдеры — плагины, которые знают как работать с определённым API:

| Провайдер | Для чего | Используется в |
|---|---|---|
| `hashicorp/azurerm` | Azure API (AKS, Resource Groups) | k8s-cluster |
| `hashicorp/kubernetes` | Kubernetes API | k8s-app, k8s-local |
| `tehcyx/kind` | kind (Kubernetes in Docker) | k8s-local |
| `hashicorp/null` | Вспомогательный (null_resource) | k8s-local |

Terragrunt генерирует блоки `provider {}` автоматически и передаёт в модули нужные параметры.

---

## Жизненный цикл ресурсов Terraform

```
terraform init
    ↓ скачивает провайдеры в .terraform/

terraform plan
    ↓ читает .tf файлы
    ↓ сравнивает с state
    ↓ показывает: + create, ~ update, - destroy

terraform apply
    ↓ создаёт/обновляет ресурсы
    ↓ сохраняет state (локально или в Azure Blob)

terraform destroy
    ↓ удаляет все ресурсы из state
```

**State** хранится в `terraform.tfstate` (локально) или в Azure Blob Storage (prod). Никогда не удаляйте state вручную — это приведёт к «осиротевшим» ресурсам (созданным в облаке, но о которых Terraform «забыл»).

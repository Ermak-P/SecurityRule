# Локальный Kubernetes с Terraform + Terragrunt

Это руководство описывает, как поднять **полный стек SecurityRule** (приложение + SQL Server) в локальном Kubernetes кластере на вашей машине — без облака, без платных ресурсов.

Используемые технологии:
- **kind** (Kubernetes IN Docker) — лёгкий локальный k8s
- **Terraform** — описывает ресурсы (кластер, namespace, deployments, services)
- **Terragrunt** — оркестрирует конфигурации окружений, управляет state

---

## Оглавление

1. [Предварительные требования](#1-предварительные-требования)
2. [Структура файлов](#2-структура-файлов)
3. [Пошаговый запуск](#3-пошаговый-запуск)
4. [Проверка работы](#4-проверка-работы)
5. [Остановка и удаление](#5-остановка-и-удаление)
6. [Частые проблемы](#6-частые-проблемы)
7. [Переменные конфигурации](#7-переменные-конфигурации)

---

## 1. Предварительные требования

Установите следующие инструменты (один раз):

### Docker Desktop
Скачайте с https://www.docker.com/products/docker-desktop и запустите.

### kind
```bash
# macOS
brew install kind

# Linux
curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.22.0/kind-linux-amd64
chmod +x ./kind && sudo mv ./kind /usr/local/bin/kind

# Windows (PowerShell, от администратора)
winget install Kubernetes.kind
```
Проверка: `kind version`

### kubectl
```bash
# macOS
brew install kubectl

# Linux
curl -LO "https://dl.k8s.io/release/$(curl -sL https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# Windows
winget install Kubernetes.kubectl
```
Проверка: `kubectl version --client`

### Terraform ≥ 1.6
```bash
# macOS / Linux (через tfenv)
brew install tfenv
tfenv install 1.8.0
tfenv use 1.8.0

# Или скачайте бинарник: https://developer.hashicorp.com/terraform/downloads
```
Проверка: `terraform version`

### Terragrunt ≥ 0.55
```bash
# macOS
brew install terragrunt

# Linux
wget -O terragrunt https://github.com/gruntwork-io/terragrunt/releases/latest/download/terragrunt_linux_amd64
chmod +x terragrunt && sudo mv terragrunt /usr/local/bin/

# Windows
winget install Gruntwork.Terragrunt
```
Проверка: `terragrunt --version`

---

## 2. Структура файлов

```
terraform/
└── modules/
    └── k8s-local/              ← Terraform-модуль для локального кластера
        ├── main.tf             ← kind кластер + SQL Server + приложение
        ├── variables.tf        ← все настройки (порты, образ, пароль)
        └── outputs.tf          ← URL приложения, путь к kubeconfig

terragrunt/
└── environments/
    └── local/
        ├── env.hcl             ← environment=local
        └── k8s-local/
            └── terragrunt.hcl  ← local backend, inputs
```

---

## 3. Пошаговый запуск

### Шаг 1 — Клонируйте репозиторий (если ещё не сделано)
```bash
git clone https://github.com/Ermak-P/SecurityRule.git
cd SecurityRule
```

### Шаг 2 — Соберите Docker образ приложения
```bash
# Из корня репозитория (там где Dockerfile)
docker build -t security-rule:local .
```
> Образ получит тег `security-rule:local` — именно это имя прописано в Terraform конфиге.

### Шаг 3 — Инициализируйте Terraform провайдеры
```bash
cd terragrunt/environments/local/k8s-local
terragrunt init
```
При первом запуске Terragrunt скачает провайдеры:
- `tehcyx/kind` — управляет kind кластером
- `hashicorp/kubernetes` — деплоит ресурсы
- `hashicorp/null` — вспомогательный

### Шаг 4 — Проверьте план (опционально, но рекомендуется)
```bash
terragrunt plan
```
Вы увидите список ресурсов, которые будут созданы:
- `kind_cluster.this` — кластер с пробросом портов 8080 и 1433
- `kubernetes_namespace.this` — namespace `security-rule`
- `kubernetes_stateful_set.mssql` — SQL Server 2022
- `kubernetes_service.mssql` — NodePort сервис для SQL Server
- `kubernetes_deployment.app` — приложение SecurityRule
- `kubernetes_service.app` — NodePort сервис для приложения
- секреты и ConfigMap

### Шаг 5 — Создайте кластер и задеплойте приложение
```bash
terragrunt apply
```
Когда появится вопрос `Do you want to perform these actions?` — введите `yes`.

**Что происходит за кулисами:**
1. kind создаёт Docker-контейнер с Kubernetes внутри
2. Пробрасывает порты: `localhost:8080 → pod:8080`, `localhost:1433 → pod:1433`
3. Создаётся namespace `security-rule`
4. Деплоится SQL Server 2022 Developer (бесплатная редакция)
5. Деплоится приложение SecurityRule (с init-контейнером, который ждёт готовности SQL Server)

> ⏱ Первый запуск занимает ~3-5 минут (скачиваются Docker образы).

### Шаг 6 — Загрузите локальный образ в kind
```bash
# kind не имеет доступа к вашему локальному Docker registry по умолчанию,
# поэтому образ нужно загрузить явно:
kind load docker-image security-rule:local --name security-rule-local
```
> Если вы забудете этот шаг, pod приложения не запустится с ошибкой `ErrImageNeverPull`.

### Шаг 7 — После apply
Terragrunt выведет:
```
Outputs:
app_url          = "http://localhost:8080"
cluster_name     = "security-rule-local"
kubeconfig_path  = "/home/<user>/.kube/config"
mssql_connection = "Server=localhost,1433;Database=SecurityRuleDb;..."
```

**Проверьте статус подов:**
```bash
kubectl get pods -n security-rule --watch
```
Дождитесь состояния `Running` для обоих подов (`mssql-0` и `security-rule-...`).

---

## 4. Проверка работы

### Приложение
Откройте браузер: **http://localhost:8080**

### Поды и логи
```bash
# Список подов
kubectl get pods -n security-rule

# Логи приложения
kubectl logs -n security-rule -l app=security-rule -f

# Логи SQL Server
kubectl logs -n security-rule -l app=mssql -f

# Описание пода (если что-то не запускается)
kubectl describe pod -n security-rule <pod-name>
```

### SQL Server (из IDE)
Используйте SSMS или Azure Data Studio:
- **Server**: `localhost,1433`
- **Login**: `sa`
- **Password**: `LocalDev!Passw0rd` (или ваш `TF_VAR_mssql_sa_password`)
- **Trust Certificate**: Yes

### Статус кластера
```bash
kubectl get all -n security-rule
```

---

## 5. Остановка и удаление

### Удалить все ресурсы (сохранить kind кластер)
```bash
cd terragrunt/environments/local/k8s-local
terragrunt destroy
```

### Удалить кластер полностью
```bash
kind delete cluster --name security-rule-local
```

### Пересоздать с нуля
```bash
kind delete cluster --name security-rule-local
docker build -t security-rule:local .
terragrunt apply
kind load docker-image security-rule:local --name security-rule-local
```

---

## 6. Частые проблемы

### `Error: failed to create cluster: failed to init node...`
Docker Desktop не запущен. Запустите Docker и повторите.

### Pod в состоянии `ErrImageNeverPull`
Образ не загружен в kind. Выполните:
```bash
kind load docker-image security-rule:local --name security-rule-local
kubectl rollout restart deployment/security-rule -n security-rule
```

### Pod в состоянии `CrashLoopBackOff`
Смотрите логи:
```bash
kubectl logs -n security-rule -l app=security-rule --previous
```
Частая причина: SQL Server ещё не готов. init-контейнер `wait-for-mssql` должен это предотвращать, но SQL Server иногда требует несколько минут для первого старта. Подождите и проверьте:
```bash
kubectl logs -n security-rule -l app=mssql
```

### `Error: Post "http://localhost/.../namespaces": dial tcp...`
Kubernetes провайдер не может найти кластер. Убедитесь что:
```bash
kubectl config get-contexts  # должен быть kind-security-rule-local
kubectl config use-context kind-security-rule-local
```

### Порт 8080 или 1433 уже занят
Измените порты в `terragrunt.hcl`:
```hcl
inputs = {
  host_port_app   = 9090   # вместо 8080
  host_port_mssql = 14330  # вместо 1433
}
```
Затем `terragrunt destroy && terragrunt apply`.

### `Error: provider "tehcyx/kind" not found`
```bash
terragrunt init -upgrade
```

---

## 7. Переменные конфигурации

| Переменная | По умолчанию | Описание |
|---|---|---|
| `cluster_name` | `security-rule-local` | Имя kind кластера |
| `namespace` | `security-rule` | Kubernetes namespace |
| `app_image` | `security-rule:local` | Docker образ приложения |
| `app_image_pull_policy` | `Never` | `Never` для локального образа, `IfNotPresent` для registry |
| `mssql_sa_password` | `LocalDev!Passw0rd` | Пароль SA для SQL Server |
| `development_user` | `local-dev-user` | Имя пользователя для DevelopmentAuthenticationHandler |
| `host_port_app` | `8080` | Порт хоста для приложения |
| `host_port_mssql` | `1433` | Порт хоста для SQL Server |

Переменные можно переопределить через переменные окружения:
```bash
export TF_VAR_mssql_sa_password="MySecurePass!123"
export TF_VAR_development_user="john.doe"
```
Или через файл (не коммитить!):
```bash
cat > /tmp/local.tfvars <<'EOF'
mssql_sa_password = "MySecurePass!123"
development_user  = "john.doe"
EOF
TF_CLI_ARGS_apply="-var-file=/tmp/local.tfvars" terragrunt apply
```

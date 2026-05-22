# Обзор инфраструктуры SecurityRule

Этот документ объясняет **зачем нужны** папки `k8s/`, `terraform/` и `terragrunt/`, как они связаны между собой и когда что использовать.

---

## Зачем вообще всё это нужно?

Приложение SecurityRule — это Blazor Server + SQL Server. Чтобы запустить его в «настоящей» среде (не у вас на ноутбуке через `dotnet run`), нужно:

1. **Где-то развернуть** — на сервере, в облаке, в Kubernetes
2. **Описать конфигурацию** — сколько ресурсов, какой образ, какие пароли
3. **Управлять окружениями** — dev и prod должны быть одинаковыми по структуре, но разными по мощности

Для этих задач используются три разных инструмента, работающих на разных уровнях:

```
┌─────────────────────────────────────────────────────────────┐
│  TERRAGRUNT — оркестратор конфигураций окружений            │
│  "Какое окружение? Где хранить state? Какие параметры?"      │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  TERRAFORM — описание инфраструктуры                 │   │
│  │  "Создать кластер AKS в Azure / kind в Docker"       │   │
│  │                                                      │   │
│  │  ┌───────────────────────────────────────────────┐   │   │
│  │  │  KUBERNETES (k8s) — запуск контейнеров        │   │   │
│  │  │  "Запустить приложение, SQL Server, сеть"     │   │   │
│  │  └───────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Что такое Kubernetes (k8s)?

**Kubernetes** — это система управления контейнерами. Вместо того чтобы запускать `docker run` вручную, вы описываете *желаемое состояние* в YAML-файлах, а Kubernetes сам:
- запускает нужные контейнеры
- перезапускает их, если они упали
- балансирует нагрузку между копиями
- управляет сетевым доступом

**Ключевые понятия:**
- **Pod** — минимальная единица, один или несколько контейнеров
- **Deployment** — описывает как запускать Pod (образ, порты, ресурсы, сколько копий)
- **Service** — стабильный DNS-адрес и балансировщик для подов
- **Ingress** — входная точка из интернета (аналог nginx reverse proxy)
- **ConfigMap** — несекретные настройки (переменные окружения)
- **Secret** — секретные данные (пароли, строки подключения)
- **StatefulSet** — специальный тип деплоймента с сохранением состояния (для БД)
- **PVC** — запрос на выделение диска
- **Namespace** — логическая изоляция ресурсов внутри одного кластера

---

## Что такое Terraform?

**Terraform** — инструмент «Infrastructure as Code» (IaC). Вместо ручного создания ресурсов в Azure/AWS/GCP через UI, вы описываете их в `.tf`-файлах, а Terraform:
- создаёт ресурсы (AKS кластер, диски, сети)
- обновляет их при изменении конфига
- удаляет при `terraform destroy`
- хранит **state** — запись о том, что уже создано

**В проекте Terraform делает две вещи:**
1. Создаёт Kubernetes **кластер** (AKS в облаке или kind локально)
2. Деплоит **приложение** в существующий кластер (Deployment, Service, Secrets)

---

## Что такое Terragrunt?

**Terragrunt** — «DRY-обёртка» над Terraform. Решает проблему дублирования: без него в каждом из 6 модулей пришлось бы повторять конфиг backend'а и провайдеров.

Terragrunt добавляет:
- **автоматическую генерацию** `backend.tf` и `provider.tf` для каждого модуля
- **управление зависимостями** между модулями (сначала кластер, потом приложение)
- **иерархию конфигураций** — общие настройки в `_envcommon/`, специфичные в окружениях

---

## Три сценария использования

### Сценарий 1: Локальная разработка (без облака)

```
Terragrunt (environments/local/k8s-local/) 
  → Terraform (modules/k8s-local/)
    → kind cluster в Docker
      → Kubernetes ресурсы (namespace, SQL Server, приложение)
```

**Когда использовать:** разработка на своей машине, без Azure-аккаунта.

### Сценарий 2: Dev/Prod в Azure

```
Terragrunt (environments/dev/ или prod/)
  → Terraform (modules/k8s-cluster/) → AKS кластер в Azure
  → Terraform (modules/k8s-app/)     → приложение в AKS
```

**Когда использовать:** CI/CD, staging-окружение, продакшн.

### Сценарий 3: kubectl apply вручную (без Terraform)

```
Kustomize (k8s/overlays/dev/ или prod/)
  → kubectl apply -k
    → Kubernetes ресурсы напрямую
```

**Когда использовать:** быстрая проверка, если кластер уже есть, или если Terraform не нужен.

---

## Структура папок

```
SecurityRule/
├── k8s/                        ← Kubernetes YAML-манифесты (Kustomize)
│   ├── base/                   ← базовая конфигурация
│   └── overlays/               ← изменения для dev/prod
│
├── terraform/                  ← Terraform модули (переиспользуемые)
│   └── modules/
│       ├── k8s-cluster/        ← создаёт AKS кластер в Azure
│       ├── k8s-app/            ← деплоит приложение в кластер
│       └── k8s-local/          ← создаёт kind кластер локально
│
├── terragrunt/                 ← Terragrunt конфиги окружений
│   ├── terragrunt.hcl          ← корневой конфиг (backend, providers)
│   ├── _envcommon/             ← общие inputs для всех окружений
│   └── environments/
│       ├── dev/                ← dev: 1 нода, стандартные VM
│       ├── prod/               ← prod: 3 ноды, мощные VM
│       └── local/              ← local: kind в Docker
│
└── docs/                       ← документация (вы здесь)
    ├── infrastructure-overview.md  ← этот файл
    ├── k8s.md                  ← документация k8s
    ├── terraform.md            ← документация terraform
    ├── terragrunt.md           ← документация terragrunt
    └── local-k8s.md            ← пошаговый гайд локального запуска
```

---

## Как всё работает вместе (схема потока)

```
Разработчик
    │
    ├─ cd terragrunt/environments/dev/k8s-cluster
    ├─ terragrunt apply
    │       │
    │       ├─ читает env.hcl → environment=dev, location=westeurope
    │       ├─ читает _envcommon/k8s-cluster.hcl → общие inputs
    │       ├─ генерирует backend.tf (Azure Blob Storage)
    │       ├─ генерирует provider.tf (azurerm ~> 3.0)
    │       └─ вызывает terraform/modules/k8s-cluster/
    │               │
    │               └─ создаёт в Azure:
    │                   - Resource Group rg-securityrule-dev
    │                   - AKS кластер aks-securityrule-dev (1 нода)
    │                   └─ outputs: host, client_certificate, ...
    │
    ├─ cd terragrunt/environments/dev/k8s-app
    ├─ terragrunt apply
    │       │
    │       ├─ dependency.cluster → берёт outputs из k8s-cluster
    │       ├─ генерирует provider_kubernetes.tf с реквизитами кластера
    │       └─ вызывает terraform/modules/k8s-app/
    │               │
    │               └─ деплоит в AKS:
    │                   - Namespace security-rule
    │                   - ConfigMap app-config
    │                   - Secret app-secret (строки подключения)
    │                   - Secret mssql-secret (пароль БД)
    │                   - Deployment security-rule (1 реплика)
    │                   └─ Service security-rule
    │
    └─ Приложение доступно через Ingress
```

---

## Подробная документация

| Файл | Что описано |
|---|---|
| [k8s.md](./k8s.md) | Все YAML-файлы в `k8s/`, объекты Kubernetes, Kustomize |
| [terraform.md](./terraform.md) | Все `.tf`-файлы, модули k8s-cluster, k8s-app, k8s-local |
| [terragrunt.md](./terragrunt.md) | Вся структура `terragrunt/`, окружения dev/prod/local |
| [local-k8s.md](./local-k8s.md) | Пошаговый гайд запуска локального кластера |

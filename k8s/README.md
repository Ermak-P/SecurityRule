# Kubernetes manifests — SecurityRule

## Структура

```
k8s/
├── base/                          # Базовые манифесты (namespace-agnostic конфигурация)
│   ├── kustomization.yaml
│   ├── namespace.yaml             # Namespace: security-rule
│   ├── app/
│   │   ├── configmap.yaml         # Не-секретные переменные окружения
│   │   ├── secret.yaml            # Строки подключения к БД (содержат пароль)
│   │   ├── deployment.yaml        # Deployment для Blazor Server приложения
│   │   ├── service.yaml           # ClusterIP Service (порт 80 → 8080)
│   │   └── ingress.yaml           # Ingress с sticky sessions (нужны для SignalR)
│   └── db/
│       ├── secret.yaml            # SA-пароль SQL Server
│       ├── pvc.yaml               # PersistentVolumeClaim (10Gi) для данных БД
│       ├── statefulset.yaml       # StatefulSet для SQL Server 2022
│       └── service.yaml           # Headless Service для SQL Server
└── overlays/
    ├── dev/
    │   └── kustomization.yaml     # Dev: 1 реплика, dev-user
    └── prod/
        └── kustomization.yaml     # Prod: 2 реплики, prod hostname
```

## Предварительные требования

- Kubernetes кластер (1.24+)
- kubectl
- kustomize (встроен в kubectl 1.14+)
- Установленный Ingress Controller (nginx)
- Образ собранный и опубликованный в registry

## Сборка Docker образа

```bash
docker build -t ghcr.io/ermak-p/securityrule:latest .
docker push ghcr.io/ermak-p/securityrule:latest
```

## Подготовка секретов

Перед деплоем необходимо задать реальные пароли в двух файлах:

**`k8s/base/db/secret.yaml`** — пароль SA для SQL Server:
```yaml
stringData:
  sa-password: "YourStrong!Passw0rd"   # Минимум 8 символов, заглавные + строчные + цифра + спецсимвол
```

**`k8s/base/app/secret.yaml`** — строки подключения к БД:
```yaml
stringData:
  ConnectionStrings__DefaultConnection: "Server=mssql;Database=SecurityRuleDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  ConnectionStrings__FakeAdConnection:  "Server=mssql;Database=FakeAdDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
```

> ⚠️ **Не коммитьте файлы с реальными паролями в Git.** Используйте внешние менеджеры секретов (Sealed Secrets, External Secrets Operator, Vault).

## Деплой

### Dev-окружение
```bash
kubectl apply -k k8s/overlays/dev
```

### Prod-окружение
```bash
kubectl apply -k k8s/overlays/prod
```

### Только базовые манифесты
```bash
kubectl apply -k k8s/base
```

## Удаление
```bash
kubectl delete -k k8s/overlays/dev   # или prod
```

## Важные особенности

### Sticky Sessions (обязательно для Blazor Server)
Blazor Server использует SignalR (WebSocket). При нескольких репликах Ingress должен направлять повторные запросы на тот же Pod.
Это настроено в `app/ingress.yaml` через аннотации `nginx.ingress.kubernetes.io/affinity`.

### База данных
SQL Server запускается как StatefulSet с одной репликой и PVC для хранения данных.
EF Core миграции применяются автоматически при старте приложения (`db.Database.Migrate()`).

### Аутентификация
По умолчанию `Authentication__UseActiveDirectory=false` — используется `DevelopmentAuthenticationHandler`.
Для интеграции с AD Negotiate в кластере потребуется дополнительная настройка (Windows Auth / kerberos).

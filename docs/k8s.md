# Документация: папка `k8s/`

Папка `k8s/` содержит **Kubernetes-манифесты** приложения SecurityRule — YAML-файлы, которые описывают все объекты, необходимые для работы приложения в Kubernetes кластере.

Манифесты организованы с помощью **Kustomize** — встроенного в `kubectl` инструмента, который позволяет иметь базовую конфигурацию и накладывать поверх неё изменения для конкретных окружений.

---

## Структура папки

```
k8s/
├── README.md                      ← краткое описание
├── base/                          ← базовая конфигурация (общая для всех окружений)
│   ├── kustomization.yaml         ← список всех файлов базы
│   ├── namespace.yaml             ← Namespace — логическая изоляция
│   ├── app/                       ← ресурсы Blazor-приложения
│   │   ├── deployment.yaml        ← Deployment — как запускать приложение
│   │   ├── service.yaml           ← Service — сетевой доступ внутри кластера
│   │   ├── ingress.yaml           ← Ingress — внешний доступ из интернета
│   │   ├── configmap.yaml         ← ConfigMap — несекретные переменные окружения
│   │   └── secret.yaml            ← Secret — строки подключения к БД
│   └── db/                        ← ресурсы SQL Server
│       ├── statefulset.yaml       ← StatefulSet — запуск SQL Server с данными
│       ├── service.yaml           ← Service — сетевой доступ к БД внутри кластера
│       ├── pvc.yaml               ← PVC — запрос дискового пространства
│       └── secret.yaml            ← Secret — пароль SA для SQL Server
└── overlays/                      ← "наложения" поверх базы
    ├── dev/                       ← настройки для dev-окружения
    │   └── kustomization.yaml     ← 1 реплика, dev-пользователь
    └── prod/                      ← настройки для prod-окружения
        └── kustomization.yaml     ← 2 реплики, prod-хостнейм
```

---

## Как применить манифесты

```bash
# Применить dev-конфигурацию (нужен уже работающий кластер)
kubectl apply -k k8s/overlays/dev

# Применить prod-конфигурацию
kubectl apply -k k8s/overlays/prod

# Просмотреть итоговый YAML без применения
kubectl kustomize k8s/overlays/dev
```

> **Примечание:** При использовании Terraform/Terragrunt `kubectl apply` не нужен — Terraform сам создаёт все эти ресурсы через Kubernetes provider. Папка `k8s/` полезна как альтернативный способ деплоя или как справочник по структуре объектов.

---

## `k8s/base/namespace.yaml` — Пространство имён

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: security-rule
```

### Зачем нужен Namespace?

**Namespace** — это логический «контейнер» внутри кластера. Все объекты приложения (поды, сервисы, секреты) создаются в namespace `security-rule`, что:

- **изолирует** их от системных подов (kube-system) и других приложений
- позволяет задавать **квоты ресурсов** на уровне namespace
- упрощает **удаление** — можно удалить весь namespace одной командой
- улучшает **RBAC** — можно выдавать права на конкретный namespace

Без namespace все ресурсы попали бы в `default`, что затрудняет управление.

---

## `k8s/base/kustomization.yaml` — Список ресурсов базы

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - namespace.yaml
  - app/configmap.yaml
  - app/secret.yaml
  - app/deployment.yaml
  - app/service.yaml
  - app/ingress.yaml
  - db/secret.yaml
  - db/pvc.yaml
  - db/statefulset.yaml
  - db/service.yaml
```

### Зачем нужен kustomization.yaml?

Это «оглавление» для Kustomize — он знает, какие файлы входят в набор. При запуске `kubectl apply -k k8s/base` или `kubectl kustomize k8s/base` Kustomize читает именно этот файл и собирает все перечисленные ресурсы в единый YAML.

**Порядок важен** — namespace должен быть первым, чтобы другие ресурсы могли ссылаться на него.

---

## `k8s/base/app/deployment.yaml` — Развёртывание приложения

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: security-rule
  namespace: security-rule
  labels:
    app: security-rule
spec:
  replicas: 1
  selector:
    matchLabels:
      app: security-rule
  template:
    metadata:
      labels:
        app: security-rule
    spec:
      containers:
        - name: security-rule
          image: ghcr.io/ermak-p/securityrule:latest
          imagePullPolicy: Always
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: app-config
            - secretRef:
                name: app-secret
          livenessProbe:
            httpGet:
              path: /
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 15
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /
              port: 8080
            initialDelaySeconds: 15
            periodSeconds: 10
            failureThreshold: 3
          resources:
            requests:
              memory: "256Mi"
              cpu: "100m"
            limits:
              memory: "512Mi"
              cpu: "500m"
```

### Разбор полей

| Поле | Значение | Описание |
|---|---|---|
| `replicas: 1` | 1 | Сколько одновременно работающих копий пода |
| `selector.matchLabels` | `app: security-rule` | По этому лейблу Deployment находит «свои» поды |
| `image` | `ghcr.io/ermak-p/securityrule:latest` | Docker-образ из GitHub Container Registry |
| `imagePullPolicy: Always` | Always | Всегда скачивать свежий образ (важно для `:latest`) |
| `containerPort: 8080` | 8080 | Порт, на котором слушает ASP.NET Core внутри контейнера |
| `envFrom.configMapRef` | app-config | Все ключи из ConfigMap превращаются в переменные окружения |
| `envFrom.secretRef` | app-secret | Все ключи из Secret превращаются в переменные окружения |

### livenessProbe — проверка «жив ли под»

```yaml
livenessProbe:
  httpGet:
    path: /
    port: 8080
  initialDelaySeconds: 30   # ждать 30с после старта перед первой проверкой
  periodSeconds: 15          # проверять каждые 15 секунд
  failureThreshold: 3        # после 3 неудач — перезапустить под
```

**Зачем:** если приложение зависло или упало, Kubernetes автоматически перезапустит под. Без liveness probe «зависший» процесс будет продолжать жить, не обрабатывая запросы.

### readinessProbe — проверка «готов ли принимать трафик»

```yaml
readinessProbe:
  httpGet:
    path: /
    port: 8080
  initialDelaySeconds: 15   # ждать 15с перед первой проверкой (время старта)
  periodSeconds: 10          # проверять каждые 10 секунд
  failureThreshold: 3        # после 3 неудач — убрать из балансировки
```

**Зачем:** пока приложение стартует (загружает данные, инициализирует соединение с БД), оно ещё не готово обрабатывать запросы. Readiness probe гарантирует, что трафик пойдёт на под только когда он действительно готов.

**Разница от liveness:** liveness = «перезапустить», readiness = «убрать из балансировки».

### resources — ограничения ресурсов

```yaml
resources:
  requests:
    memory: "256Mi"   # гарантировано выделить 256 MB RAM
    cpu: "100m"       # гарантировано 100 millicores (0.1 CPU)
  limits:
    memory: "512Mi"   # максимум 512 MB RAM
    cpu: "500m"       # максимум 0.5 CPU
```

- `requests` — минимум, который Kubernetes резервирует при планировании пода на ноду
- `limits` — максимум, больше которого под не получит (при превышении memory — OOM kill)
- `100m` CPU = 100 millicores = 0.1 ядра процессора

---

## `k8s/base/app/configmap.yaml` — Несекретные настройки приложения

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
  namespace: security-rule
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ASPNETCORE_URLS: "http://+:8080"
  Authentication__UseActiveDirectory: "false"
  Authentication__DevelopmentUser: "k8s-user"
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft.AspNetCore: "Warning"
```

### Зачем использовать ConfigMap вместо dockerfile ENV?

- Настройки можно **изменить без пересборки образа** — просто обновить ConfigMap и перезапустить поды
- Разные окружения используют **разные значения** одних и тех же переменных
- Секретные данные отделены в Secret

### Объяснение переменных

| Переменная | Значение | Назначение |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Режим ASP.NET Core — влияет на уровень логирования, обработку ошибок |
| `ASPNETCORE_URLS` | `http://+:8080` | Приложение слушает на всех интерфейсах на порту 8080 |
| `Authentication__UseActiveDirectory` | `false` | Не использовать реальный AD (Negotiate) |
| `Authentication__DevelopmentUser` | `k8s-user` | Имя фиктивного пользователя для DevelopmentAuthenticationHandler |
| `Logging__LogLevel__Default` | `Information` | Писать логи уровня Info и выше |
| `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | Для ASP.NET Core писать только Warning и выше (меньше шума) |

> Двойное подчёркивание `__` в именах переменных окружения = разделитель иерархии в `appsettings.json`. `Authentication__UseActiveDirectory` = `Authentication.UseActiveDirectory`.

---

## `k8s/base/app/secret.yaml` — Строки подключения к БД

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-secret
  namespace: security-rule
type: Opaque
stringData:
  # Connection strings reference the mssql Service DNS name.
  # Replace <SA_PASSWORD> with the same password used in db/secret.yaml.
  ConnectionStrings__DefaultConnection: "Server=mssql;Database=SecurityRuleDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True;"
  ConnectionStrings__FakeAdConnection: "Server=mssql;Database=FakeAdDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True;"
```

### Зачем Secret, а не ConfigMap?

Secrets хранятся в etcd в base64-кодировке и могут быть:
- зашифрованы at-rest в etcd (если включено)
- исключены из логов
- защищены отдельными RBAC-правами

### Почему `Server=mssql`?

Внутри кластера Kubernetes каждый Service получает DNS-имя: `<service-name>.<namespace>.svc.cluster.local`. Для краткости можно использовать просто `mssql` (если оба пода в одном namespace).

### ⚠️ Важно для продакшена

Файл `secret.yaml` **не должен содержать реальные пароли в git**. Используйте:
- **External Secrets Operator** — синхронизирует секреты из Azure Key Vault / AWS Secrets Manager
- **Sealed Secrets** — шифрует секреты для git
- **CI/CD переменные** — передаются через pipeline без хранения в коде

---

## `k8s/base/app/service.yaml` — Сетевой доступ к приложению

```yaml
apiVersion: v1
kind: Service
metadata:
  name: security-rule
  namespace: security-rule
spec:
  selector:
    app: security-rule   # находит поды с этим лейблом
  ports:
    - port: 80           # порт снаружи (внутри кластера)
      targetPort: 8080   # пробрасывается на порт контейнера
      protocol: TCP
```

### Зачем нужен Service?

Поды создаются и пересоздаются с разными IP-адресами. Service предоставляет **стабильный DNS-адрес** и **балансировку нагрузки** между подами.

Приложение доступно как:
- `security-rule` (в том же namespace)
- `security-rule.security-rule` (краткий вариант)
- `security-rule.security-rule.svc.cluster.local` (полный FQDN)

Тип не указан → по умолчанию `ClusterIP` — только внутри кластера.

---

## `k8s/base/app/ingress.yaml` — Внешний доступ из интернета

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: security-rule
  namespace: security-rule
  annotations:
    # Sticky sessions are required for Blazor Server (SignalR WebSocket connections).
    nginx.ingress.kubernetes.io/affinity: "cookie"
    nginx.ingress.kubernetes.io/session-cookie-name: "security-rule-session"
    nginx.ingress.kubernetes.io/session-cookie-expires: "172800"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
spec:
  ingressClassName: nginx
  rules:
    - host: security-rule.example.com   # Replace with your actual hostname
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: security-rule
                port:
                  number: 80
```

### Зачем нужен Ingress?

Service типа ClusterIP недоступен снаружи кластера. Ingress — это «входная точка», аналог nginx reverse proxy, который:
- принимает HTTP-запросы снаружи
- маршрутизирует по хостнейму и пути
- может делать TLS-терминацию (HTTPS)

### Sticky sessions — почему они критичны для Blazor?

Blazor Server работает через **SignalR** (WebSocket). Соединение между браузером и конкретным подом должно быть постоянным — если следующий запрос попадёт на другой под, SignalR-сессия оборвётся.

```yaml
nginx.ingress.kubernetes.io/affinity: "cookie"
nginx.ingress.kubernetes.io/session-cookie-name: "security-rule-session"
```

Nginx запоминает, на какой под направил первый запрос, и использует cookie для закрепления последующих запросов за тем же подом.

### Таймауты

```yaml
nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"   # 1 час
nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"   # 1 час
```

WebSocket-соединения долгоживущие. Стандартный таймаут nginx (60 секунд) закроет соединение раньше времени. 3600 секунд = 1 час бездействия.

---

## `k8s/base/db/statefulset.yaml` — SQL Server

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mssql
  namespace: security-rule
spec:
  selector:
    matchLabels:
      app: mssql
  serviceName: mssql
  replicas: 1
  template:
    spec:
      containers:
        - name: mssql
          image: mcr.microsoft.com/mssql/server:2022-latest
          ports:
            - containerPort: 1433
          env:
            - name: ACCEPT_EULA
              value: "Y"
            - name: MSSQL_SA_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: mssql-secret
                  key: sa-password
          volumeMounts:
            - name: mssql-data
              mountPath: /var/opt/mssql
          resources:
            requests:
              memory: "2Gi"
              cpu: "500m"
            limits:
              memory: "4Gi"
              cpu: "2"
      volumes:
        - name: mssql-data
          persistentVolumeClaim:
            claimName: mssql-pvc
```

### StatefulSet vs Deployment

| | Deployment | StatefulSet |
|---|---|---|
| Имя пода | случайное (`app-xyz123`) | стабильное (`mssql-0`) |
| Сетевая идентичность | меняется | постоянная |
| Порядок старта/остановки | произвольный | строгий (по индексу) |
| Тома | общие или отдельные | гарантировано отдельные |
| **Используется для** | stateless приложений | баз данных, очередей |

SQL Server — stateful, поэтому StatefulSet: при перезапуске `mssql-0` получит **тот же том** с данными.

### Пароль из Secret

```yaml
- name: MSSQL_SA_PASSWORD
  valueFrom:
    secretKeyRef:
      name: mssql-secret
      key: sa-password
```

Пароль не прописан в явном виде — берётся из Secret `mssql-secret`, ключ `sa-password`. Контейнер получает его как переменную окружения.

### Ресурсы SQL Server

```yaml
requests:
  memory: "2Gi"    # SQL Server требует минимум 2 GB RAM
  cpu: "500m"      # 0.5 ядра гарантировано
limits:
  memory: "4Gi"    # максимум 4 GB
  cpu: "2"         # максимум 2 ядра
```

Это минимальные требования SQL Server 2022 для работы.

---

## `k8s/base/db/service.yaml` — Сетевой доступ к SQL Server

```yaml
apiVersion: v1
kind: Service
metadata:
  name: mssql
  namespace: security-rule
spec:
  selector:
    app: mssql
  ports:
    - port: 1433
      targetPort: 1433
  clusterIP: None  # Headless service — app connects by DNS name "mssql"
```

### Headless Service (`clusterIP: None`)

Обычный Service имеет виртуальный IP и балансирует трафик. `clusterIP: None` создаёт **Headless Service**:
- DNS возвращает напрямую IP пода (не виртуальный IP)
- Стабильное DNS-имя `mssql` разрешается в IP пода `mssql-0`
- Необходимо для StatefulSet — иначе имя каждого пода не будет разрешаться

Приложение подключается к `mssql` (или `mssql.security-rule.svc.cluster.local`).

---

## `k8s/base/db/pvc.yaml` — Постоянный диск для данных SQL Server

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mssql-pvc
  namespace: security-rule
spec:
  accessModes:
    - ReadWriteOnce   # только один под может писать одновременно
  resources:
    requests:
      storage: 10Gi   # запросить 10 GB
```

### Как работает PVC?

1. **PVC (PersistentVolumeClaim)** — «заявка» на диск. Указываем сколько GB и какой режим доступа нужен.
2. **PV (PersistentVolume)** — реальный диск. В облаке создаётся автоматически (Azure Disk, AWS EBS).
3. **StorageClass** — определяет тип диска. По умолчанию использует стандартный класс кластера.

**AccessModes:**
- `ReadWriteOnce` — один под пишет/читает (для большинства БД)
- `ReadOnlyMany` — много подов только читают
- `ReadWriteMany` — много подов пишут/читают (нужен NFS или Azure Files)

Если под `mssql-0` перезапустится — данные на диске сохранятся. Если удалить StatefulSet — PVC и данные **не удаляются** автоматически (защита от случайного удаления).

---

## `k8s/base/db/secret.yaml` — Пароль SA для SQL Server

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mssql-secret
  namespace: security-rule
type: Opaque
stringData:
  # SA password — must be at least 8 chars, contain uppercase, lowercase, digit, and special char.
  # Replace with a strong password before deploying.
  sa-password: "<SA_PASSWORD>"
```

> ⚠️ Замените `<SA_PASSWORD>` перед деплоем. SQL Server требует пароль: минимум 8 символов, заглавные + строчные буквы + цифра + спецсимвол. Иначе контейнер не запустится.

---

## `k8s/overlays/dev/kustomization.yaml` — Dev-оверлей

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - ../../base   # включить всё из base

patches:
  # 1 реплика (и так по умолчанию, но явно для документации)
  - patch: |-
      - op: replace
        path: /spec/replicas
        value: 1
    target:
      kind: Deployment
      name: security-rule
  
  # dev пользователь для аутентификации
  - patch: |-
      - op: replace
        path: /data/Authentication__DevelopmentUser
        value: "dev-user"
    target:
      kind: ConfigMap
      name: app-config
```

### Как работает Kustomize patch?

Patches используют **JSON Patch** (RFC 6902) или **Strategic Merge Patch**. Здесь JSON Patch:
- `op: replace` — заменить значение по пути
- `path: /spec/replicas` — путь в YAML (слэши вместо точек)
- `value: 1` — новое значение

Kustomize возьмёт базовый `deployment.yaml` и применит это изменение — **не нужно дублировать весь файл**.

---

## `k8s/overlays/prod/kustomization.yaml` — Prod-оверлей

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - ../../base

patches:
  # 2 реплики для отказоустойчивости
  - patch: |-
      - op: replace
        path: /spec/replicas
        value: 2
    target:
      kind: Deployment
      name: security-rule
  
  # Реальный продовый хостнейм
  - patch: |-
      - op: replace
        path: /spec/rules/0/host
        value: "security-rule.your-domain.com"
    target:
      kind: Ingress
      name: security-rule
```

### Почему 2 реплики в prod?

- **Отказоустойчивость** — если один под упадёт, второй продолжает принимать трафик
- **Rolling updates** — при обновлении один под обновляется, другой продолжает работать
- **Распределение нагрузки** — Ingress с sticky sessions балансирует между подами

> ⚠️ При 2 репликах **обязательны sticky sessions** (настроены в Ingress), иначе Blazor SignalR будет рваться при переключении между подами.

---

## Команды для работы с k8s ресурсами

```bash
# Посмотреть все поды в namespace
kubectl get pods -n security-rule

# Посмотреть все ресурсы
kubectl get all -n security-rule

# Логи приложения (с обновлением в реальном времени)
kubectl logs -n security-rule deployment/security-rule -f

# Логи SQL Server
kubectl logs -n security-rule statefulset/mssql -f

# Описание пода (если что-то не так)
kubectl describe pod -n security-rule <pod-name>

# Перезапустить деплоймент
kubectl rollout restart deployment/security-rule -n security-rule

# Статус обновления
kubectl rollout status deployment/security-rule -n security-rule

# Войти в контейнер
kubectl exec -it -n security-rule deployment/security-rule -- /bin/bash

# Проверить переменные окружения в поде
kubectl exec -n security-rule deployment/security-rule -- env | grep ConnectionString

# Применить изменения
kubectl apply -k k8s/overlays/dev

# Удалить всё в namespace
kubectl delete namespace security-rule
```

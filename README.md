# SecurityRule

**SecurityRule** — веб-приложение на Blazor Server для управления инфраструктурой безопасности: серверами, сервисами, сертификатами, сетевыми связями, пользователями и группами AD.

---

## Содержание

1. [Требования](#1-требования)
2. [Быстрый старт — локальная разработка](#2-быстрый-старт--локальная-разработка)
3. [Запуск через Docker](#3-запуск-через-docker)
4. [Запуск в Kubernetes (локально)](#4-запуск-в-kubernetes-локально)
5. [Запуск в Azure (dev/prod)](#5-запуск-в-azure-devprod)
6. [Аутентификация](#6-аутентификация)
7. [Обзор интерфейса](#7-обзор-интерфейса)
8. [Работа с разделами](#8-работа-с-разделами)
9. [Примеры типовых сценариев](#9-примеры-типовых-сценариев)
10. [Тесты](#10-тесты)

---

## 1. Требования

### Локальная разработка (`dotnet run`)

| Инструмент | Версия | Назначение |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Компиляция и запуск |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019+ | База данных (или Docker-образ) |

Альтернатива SQL Server без установки:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPass!123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### Docker / Kubernetes

| Инструмент | Версия |
|---|---|
| Docker Desktop | последняя |
| kind | ≥ 0.22 |
| kubectl | любая |
| Terraform | ≥ 1.6 |
| Terragrunt | ≥ 0.55 |

---

## 2. Быстрый старт — локальная разработка

### Шаг 1 — Клонировать репозиторий

```bash
git clone https://github.com/Ermak-P/SecurityRule.git
cd SecurityRule
```

### Шаг 2 — Настроить строку подключения

Откройте файл `src/SecurityRule.Web/appsettings.json` и укажите вашу строку подключения:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SecurityRuleDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "FakeAdConnection": "Server=localhost;Database=FakeAdDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Для разработки создайте (или измените) файл `src/SecurityRule.Web/appsettings.Development.json`:

```json
{
  "Authentication": {
    "UseActiveDirectory": false,
    "DevelopmentUser": "developer"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SecurityRuleDb;User Id=sa;******;TrustServerCertificate=True;",
    "FakeAdConnection": "Server=localhost;Database=FakeAdDb;User Id=sa;******;TrustServerCertificate=True;"
  }
}
```

> **Важно:** При `UseActiveDirectory: false` приложение автоматически авторизует пользователя с именем из `DevelopmentUser`. Active Directory не требуется.

### Шаг 3 — Запустить

```bash
cd src/SecurityRule.Web
dotnet run
```

Приложение автоматически применит миграции БД при первом запуске.

Откройте браузер: **https://localhost:5001** или **http://localhost:5000**

---

## 3. Запуск через Docker

### Собрать образ

```bash
# Из корня репозитория
docker build -t security-rule:local .
```

### Запустить (с уже работающим SQL Server на хосте)

```bash
docker run -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal;Database=SecurityRuleDb;User Id=sa;******;TrustServerCertificate=True;" \
  -e "ConnectionStrings__FakeAdConnection=Server=host.docker.internal;Database=FakeAdDb;User Id=sa;******;TrustServerCertificate=True;" \
  -e "Authentication__UseActiveDirectory=false" \
  -e "Authentication__DevelopmentUser=developer" \
  security-rule:local
```

Откройте браузер: **http://localhost:8080**

### Docker Compose (приложение + SQL Server)

```yaml
# docker-compose.yml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourPass!123"
    ports:
      - "1433:1433"

  app:
    image: security-rule:local
    depends_on:
      - db
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Server=db;Database=SecurityRuleDb;User Id=sa;******;TrustServerCertificate=True;"
      ConnectionStrings__FakeAdConnection: "Server=db;Database=FakeAdDb;User Id=sa;******;TrustServerCertificate=True;"
      Authentication__UseActiveDirectory: "false"
      Authentication__DevelopmentUser: "developer"
```

```bash
docker build -t security-rule:local .
docker compose up
```

---

## 4. Запуск в Kubernetes (локально)

Подробный пошаговый гайд: **[docs/local-k8s.md](docs/local-k8s.md)**

Краткая инструкция:

```bash
# 1. Установить Docker Desktop, kind, kubectl, Terraform, Terragrunt

# 2. Собрать образ
docker build -t security-rule:local .

# 3. Инициализировать и применить Terraform
cd terragrunt/environments/local/k8s-local
terragrunt init
terragrunt apply   # ввести "yes" на вопрос подтверждения

# 4. Загрузить образ в kind-кластер
kind load docker-image security-rule:local --name security-rule-local

# 5. Дождаться запуска подов
kubectl get pods -n security-rule --watch
```

Приложение будет доступно по адресу: **http://localhost:8080**

---

## 5. Запуск в Azure (dev/prod)

Подробная документация:
- [docs/infrastructure-overview.md](docs/infrastructure-overview.md) — обзор инфраструктуры
- [docs/terraform.md](docs/terraform.md) — Terraform-модули
- [docs/terragrunt.md](docs/terragrunt.md) — конфигурация окружений

```bash
# Dev-окружение
cd terragrunt/environments/dev/k8s-cluster
terragrunt apply   # создаёт AKS кластер в Azure

cd ../k8s-app
terragrunt apply   # деплоит приложение в AKS
```

---

## 6. Аутентификация

### Режим разработки (без AD)

В `appsettings.Development.json` установите:
```json
{
  "Authentication": {
    "UseActiveDirectory": false,
    "DevelopmentUser": "developer"
  }
}
```
Каждый запрос автоматически аутентифицируется от имени пользователя `developer`.

### Продакшн (Active Directory / Negotiate)

В `appsettings.json` установите:
```json
{
  "Authentication": {
    "UseActiveDirectory": true
  },
  "ActiveDirectory": {
    "Domain": "corp.example.com",
    "LdapPath": "LDAP://corp.example.com",
    "UserName": "svc-account",
    "Password": "secret"
  }
}
```
Аутентификация выполняется по протоколу Kerberos/NTLM (Windows Negotiate). Пользователь входит автоматически по учётным данным Windows.

---

## 7. Обзор интерфейса

После входа вы попадаете на **Дашборд** — главную страницу со статистикой.

### Навигация (боковое меню)

| Раздел | URL | Описание |
|---|---|---|
| Дашборд | `/` | Сводка: счётчики, предупреждения по сертификатам, последние данные |
| **ИНФРАСТРУКТУРА** | | |
| Серверы | `/servers` | Список серверов (название, IP, ОС, сервисы) |
| Сервисы | `/services` | Список приложений/сервисов |
| **БЕЗОПАСНОСТЬ** | | |
| Сертификаты | `/certificates` | SSL/TLS сертификаты с датами и статусами |
| Связи | `/connections` | Сетевые связи между серверами и сервисами |
| Карта связей | `/connections/map` | Интерактивная граф-карта связей |
| **УЧЁТНЫЕ ЗАПИСИ** | | |
| Пользователи | `/users` | Учётные записи AD |
| Группы | `/groups` | Группы AD |

### Дашборд

На главной странице отображается:
- **4 счётчика**: Серверов / Сервисов / Сертификатов / Связей (кликабельны — ведут в разделы)
- **Блок предупреждений**: истёкшие сертификаты (красный) и истекающие в ближайшие 30 дней (жёлтый)
- **Последние серверы** (таблица с 6 записями)
- **Ближайшие к истечению сертификаты** (таблица с 6 записями)

---

## 8. Работа с разделами

### Серверы (`/servers`)

**Поля сервера:**
- **Название** — имя хоста (обязательно)
- **IP адрес** — IPv4/IPv6 адрес (обязательно)
- **Операционная система** — выбор из предустановленного списка с автодополнением
- **Описание** — произвольный текст
- **Сервисы** — привязка к одному или нескольким сервисам
- **Теги** — произвольные метки для группировки (используются в карте связей)

**Действия:**
- **Создать** — кнопка `+` на странице списка → `/servers/create`
- **Просмотреть** — клик по строке → `/servers/{id}`
- **Редактировать** — кнопка редактирования в карточке → `/servers/{id}/edit`
- **Удалить** — кнопка удаления в карточке
- **Клонировать** — создаёт копию сервера: `/servers/create?CloneFrom={id}`

---

### Сервисы (`/services`)

**Поля сервиса:**
- **Название** — имя сервиса (обязательно)
- **Порт** — TCP/UDP порт
- **Серверы** — серверы, на которых работает сервис (множественный выбор)
- **AD учётная запись** — системная учётная запись Windows под которой запущен сервис
- **Теги** — метки для категоризации

**Действия:** Создать / Просмотреть / Редактировать / Удалить / Клонировать (аналогично серверам)

---

### Сертификаты (`/certificates`)

**Поля сертификата:**
- **Серийный номер (SN)** — обязательно
- **Thumbprint** — отпечаток сертификата, обязательно
- **Описание** — для чего используется
- **Номер заявки** — ссылка на заявку в системе заявок
- **Дата выдачи** — дата выпуска сертификата
- **Дата истечения** — срок действия

**Статусы в списке:**
- 🟢 **Активен** — действует более 30 дней
- 🟡 **Скоро** — истекает в ближайшие 30 дней
- 🔴 **Истёк** — срок действия прошёл

---

### Связи (`/connections`)

Описывает сетевые взаимодействия между компонентами инфраструктуры.

**Поля связи:**
- **Источник** — сервер-источник и/или сервис-источник
- **Назначение** — сервер назначения и/или сервис назначения (сервис обязателен)
- **Протокол** — HTTPS, TCP, AMQP и т.д.
- **Описание** — описание назначения связи

**Карта связей** (`/connections/map`) — интерактивный граф:
- Фильтрация по серверам (правая панель, группировка по тегам)
- Чекбокс «Показывать связанные серверы» — отображает соседей выбранных серверов (приглушённо)
- Чекбокс «Показывать порт/протокол» — подписи на рёбрах графа
- Чекбокс «Показывать IP сервера» — IP-адреса в названиях узлов
- Кнопки «Выбрать все» / «Снять все»
- Настройки сохраняются в `localStorage` браузера

---

### Пользователи (`/users`)

AD учётные записи, используемые сервисами.

**Поля пользователя:**
- **Имя** — логин/имя учётной записи
- **Описание**
- **Сертификат** — привязанный сертификат (опционально)

---

### Группы (`/groups`)

Группы Active Directory.

**Поля группы:**
- **Название**
- **Описание**

---

## 9. Примеры типовых сценариев

### Сценарий 1: Добавление нового сервера

1. Перейдите в **Серверы** (`/servers`)
2. Нажмите кнопку `+` (Добавить)
3. Заполните поля:
   - Название: `web-server-01`
   - IP адрес: `192.168.1.10`
   - Операционная система: `Windows Server 2022`
   - Описание: `Основной веб-сервер`
4. Привяжите сервисы, если они уже созданы
5. Добавьте теги: `prod`, `web`
6. Нажмите **Сохранить**

---

### Сценарий 2: Регистрация нового сервиса и привязка к серверу

1. Перейдите в **Сервисы** (`/services`) → нажмите `+`
2. Заполните:
   - Название: `AuthService`
   - Порт: `8443`
   - Серверы: выберите `web-server-01`
   - AD учётная запись: выберите из списка (если создана)
   - Теги: `auth`
3. Нажмите **Сохранить**

---

### Сценарий 3: Добавление сертификата и отслеживание срока действия

1. Перейдите в **Сертификаты** (`/certificates`) → нажмите `+`
2. Заполните:
   - SN: `1A2B3C4D5E6F`
   - Thumbprint: `AB:CD:EF:...`
   - Описание: `Сертификат для AuthService`
   - Номер заявки: `REQ-2024-001`
   - Дата выдачи: `01.01.2024`
   - Дата истечения: `01.01.2025`
3. На Дашборде появится предупреждение, если сертификат истёк или истекает в течение 30 дней

---

### Сценарий 4: Описание сетевой связи между сервисами

> **Задача:** Задокументировать, что `OrderService` на сервере `app-01` обращается к `AuthService` на сервере `auth-01` по HTTPS на порт 8443.

1. Перейдите в **Связи** (`/connections`) → нажмите `+`
2. Заполните:
   - Источник (сервер): `app-01`
   - Источник (сервис): `OrderService`
   - Назначение (сервер): `auth-01`
   - Назначение (сервис): `AuthService`
   - Протокол: `HTTPS`
   - Описание: `Проверка токена JWT при каждом запросе`
3. Нажмите **Сохранить**
4. Откройте **Карту связей** (`/connections/map`) — связь отобразится на графе

---

### Сценарий 5: Анализ зависимостей сервера на карте связей

1. Откройте **Карту связей** (`/connections/map`)
2. В правой панели снимите все галочки кнопкой **Снять все**
3. Отметьте только нужный сервер (например, `auth-01`)
4. Включите чекбокс **«Показывать связанные серверы»** — на графе появятся все серверы, взаимодействующие с `auth-01`
5. Включите **«Показывать порт/протокол»** для деталей каждого соединения

---

### Сценарий 6: Клонирование сервера со схожей конфигурацией

1. Откройте карточку существующего сервера (`/servers/{id}`)
2. Нажмите кнопку **Клонировать** (создаёт копию с теми же сервисами и тегами)
3. Измените название и IP-адрес
4. Нажмите **Сохранить**

---

### Сценарий 7: Поиск по интерфейсу

Нажмите значок лупы 🔍 в верхней панели (GlobalSearch) — откроется строка поиска. Поиск работает по серверам, сервисам, сертификатам и связям.

---

## 10. Тесты

### Юнит-тесты

```bash
dotnet test src/SecurityRule.Tests/
```

### E2E тесты (Playwright)

```bash
# Сборка и установка браузера (один раз)
dotnet build src/SecurityRule.E2E.Tests/
pwsh src/SecurityRule.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps

# Запуск тестов
dotnet test src/SecurityRule.E2E.Tests/ --no-build
```

### Kubernetes-тесты

```bash
dotnet test src/SecurityRule.K8s.Tests/
```

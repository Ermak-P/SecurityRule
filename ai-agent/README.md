# .NET AI Агент

Локальный AI агент для анализа и изменения .NET/C# проектов. 
Работает полностью локально — данные не покидают ваш компьютер.

## Технологический стек

| Компонент | Технология | Описание |
|-----------|-----------|---------|
| LLM сервер | [Ollama](https://ollama.com/) | Запускает языковые модели локально |
| LLM модель | `qwen2.5-coder:7b` | Специализирована для работы с кодом |
| Агент | .NET 9 Console App | Консольное приложение на C# |
| Контейнер | Docker | Изоляция Ollama |

## Быстрый старт

### Шаг 1: Запустить Ollama

```bash
# Перейти в папку с проектом агента
cd ai-agent

# Запустить Ollama в Docker
docker compose up -d

# Проверить что запустился
docker compose logs
```

### Шаг 2: Скачать языковую модель

```bash
# Скачать модель для работы с кодом (~4.5 GB, один раз)
docker exec -it ollama ollama pull qwen2.5-coder:7b

# Проверить что модель скачана
docker exec -it ollama ollama list
```

### Шаг 3: Запустить агента

```bash
# Перейти в папку с кодом агента
cd ai-agent/src/DotnetAgent

# Запустить (указать путь до вашего .NET проекта)
dotnet run -- "C:\path\to\your\dotnet\project"

# Или запустить без аргументов — агент спросит путь сам
dotnet run
```

### Шаг 4: Работа с агентом

После запуска введите задачу в консоли:

```
> Покажи структуру проекта
> Найди все контроллеры
> Прочитай файл Program.cs
> Добавь интерфейс ILogger в класс UserService
> Найди все TODO комментарии

Служебные команды:
  история  — показать историю разговора
  очистить — очистить историю (новый контекст)
  выход    — выйти из программы
```

## Структура проекта агента

```
ai-agent/
├── docker-compose.yml           # Ollama в Docker
└── src/DotnetAgent/
    ├── DotnetAgent.csproj        # Описание проекта
    ├── Program.cs                # Точка входа
    ├── Config/
    │   └── AgentConfig.cs        # Настройки агента
    ├── Core/
    │   ├── Agent.cs              # Главный цикл агента (ReAct)
    │   └── OllamaClient.cs       # HTTP клиент для Ollama API
    ├── Models/
    │   └── OllamaModels.cs       # Модели данных Ollama API
    └── Tools/
        ├── IAgentTool.cs         # Интерфейс инструмента
        ├── ToolRegistry.cs       # Реестр инструментов
        └── FileSystemTools.cs    # Инструменты: чтение/запись файлов
```

## Как это работает

Агент использует паттерн **ReAct** (Reasoning + Acting):

```
Пользователь вводит запрос
          ↓
   Агент отправляет запрос + список инструментов в LLM
          ↓
   LLM решает: нужен ли инструмент?
     ├── Да → Агент выполняет инструмент → результат в LLM → повтор
     └── Нет → Агент показывает финальный ответ пользователю
```

**Доступные инструменты:**

| Инструмент | Описание |
|-----------|---------|
| `list_files` | Список файлов проекта |
| `read_file` | Читать содержимое файла |
| `write_file` | Изменить файл (полная замена) |
| `create_file` | Создать новый файл |
| `search_in_files` | Поиск текста в проекте |

## Требования

- **Docker Desktop** для Windows
- **.NET 9 SDK** — [скачать](https://dotnet.microsoft.com/download)
- **RAM**: минимум 8 GB (рекомендуется 16 GB)
- **Место на диске**: ~5 GB для модели

## Конфигурация

В `AgentConfig.cs` можно изменить:

```csharp
OllamaUrl = "http://localhost:11434"  // URL Ollama
ModelName = "qwen2.5-coder:7b"        // Модель
MaxToolCallsPerRequest = 10           // Лимит вызовов инструментов
```

## Следующие шаги (план развития)

Смотри [ROADMAP.md](ROADMAP.md) для полного плана развития проекта.

## Полезные команды

```bash
# Посмотреть доступные модели
docker exec -it ollama ollama list

# Скачать другую модель
docker exec -it ollama ollama pull llama3.2:3b

# Открыть Ollama веб-интерфейс (если установлен Open WebUI)
# http://localhost:3000

# Остановить Ollama
docker compose down

# Посмотреть использование ресурсов
docker stats ollama
```

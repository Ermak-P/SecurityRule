namespace DotnetAgent.Config;

/// <summary>
/// Конфигурация агента.
/// Все параметры с разумными значениями по умолчанию.
///
/// Загрузка из файла (Фаза 5):
///   var config = AgentConfig.LoadFromFileOrDefault("agent.json");
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// URL до API Ollama.
    ///
    /// Если Ollama запущен в Docker на той же машине (docker compose up -d),
    /// то это всегда http://localhost:11434
    ///
    /// Если Ollama запущен на другом компьютере в сети:
    ///   http://192.168.1.100:11434
    /// </summary>
    public string OllamaUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Название основной языковой модели в Ollama.
    ///
    /// Эта строка должна совпадать с тем что показывает команда:
    ///   docker exec -it ollama ollama list
    ///
    /// РЕКОМЕНДУЕМЫЕ МОДЕЛИ для работы с кодом:
    ///
    ///   qwen2.5-coder:7b   — рекомендуется для старта
    ///     RAM: ~5 GB | Качество: хорошее | Tool Calling: да
    ///
    ///   qwen2.5-coder:14b  — лучше качество генерации кода
    ///     RAM: ~9 GB | Качество: очень хорошее | Tool Calling: да
    ///
    ///   llama3.2:3b        — быстрая и лёгкая, подходит для слабых машин
    ///     RAM: ~2.5 GB | Качество: среднее | Tool Calling: да
    ///
    /// ВАЖНО: модель должна поддерживать "tool calling" (function calling)!
    /// </summary>
    public string ModelName { get; set; } = "qwen2.5-coder:7b";

    /// <summary>
    /// Фаза 5: быстрая лёгкая модель для простых задач.
    /// Используется когда нет инструментов (быстрые ответы).
    /// Если null — используется ModelName для всех запросов.
    /// </summary>
    public string? FastModelName { get; set; } = null;

    /// <summary>
    /// Фаза 5: умная тяжёлая модель для сложного кода.
    /// Используется для анализа и генерации сложного кода.
    /// Если null — используется ModelName для всех запросов.
    /// </summary>
    public string? SmartModelName { get; set; } = null;

    /// <summary>
    /// Абсолютный путь до .NET проекта на жёстком диске.
    ///
    /// Агент будет иметь доступ ТОЛЬКО к файлам внутри этой директории.
    /// Это предотвращает случайное изменение файлов за пределами проекта.
    ///
    /// Пример: "C:\Projects\MyWebApp"
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Максимальное количество вызовов инструментов за один запрос.
    ///
    /// Защита от бесконечного цикла.
    /// Для большинства задач достаточно 5-10 вызовов.
    /// </summary>
    public int MaxToolCallsPerRequest { get; set; } = 10;

    /// <summary>
    /// Температура генерации (0.0 — 1.0).
    ///
    /// 0.0 — детерминированный, 0.1 — для кода, 0.7 — творческий
    /// </summary>
    public float Temperature { get; set; } = 0.1f;

    /// <summary>
    /// Размер контекстного окна в токенах.
    ///
    ///   4096  — минимум, для простых задач
    ///   8192  — рекомендуется (по умолчанию)
    ///   16384 — для работы с большими файлами
    /// </summary>
    public int ContextWindowSize { get; set; } = 8192;

    /// <summary>
    /// Фаза 3: включить ли streaming вывод ответов.
    /// true = ответ выводится по мере генерации (как ChatGPT).
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Фаза 3: включить ли сохранение сессий в SQLite.
    /// true = история разговора сохраняется между запусками.
    /// </summary>
    public bool EnableSessionPersistence { get; set; } = true;

    /// <summary>
    /// Статичное описание проекта (5–20 строк), которое всегда вставляется в системный промпт.
    ///
    /// Сюда можно написать: структуру, технологии, ключевые классы, архитектурные решения.
    /// Агент будет знать контекст с первого сообщения и не будет сканировать файлы впустую.
    ///
    /// Пример:
    ///   "ASP.NET Core 8 + Blazor Server. Основные сущности: Server, AppService, ServiceConnection.
    ///    Репозитории находятся в Infrastructure/Repositories/. Всё покрыто E2E-тестами на Playwright."
    /// </summary>
    public string? ProjectContext { get; set; } = null;

    /// <summary>
    /// Свободный текст, дописываемый в конец системного промпта.
    ///
    /// Используйте для добавления специфики проекта без изменения кода агента:
    /// доменные правила, соглашения по именованию, запрещённые паттерны и т.п.
    ///
    /// Пример:
    ///   "Никогда не изменяй миграции вручную. Все классы должны иметь XML-документацию."
    /// </summary>
    public string? SystemPromptExtra { get; set; } = null;

    /// <summary>
    /// Фаза 5: URL ChromaDB для RAG.
    /// ChromaDB должен быть запущен: docker run -d -p 8000:8000 chromadb/chroma
    /// </summary>
    public string ChromaDbUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// Фаза 5: название модели для генерации embeddings (RAG).
    /// Должна быть скачана: docker exec -it ollama ollama pull nomic-embed-text
    /// </summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Возвращает подходящую модель в зависимости от задачи.
    ///
    /// Фаза 5: выбор модели — лёгкая для быстрых ответов, умная для сложного кода.
    /// </summary>
    public string GetModelForTask(ModelTask task) => task switch
    {
        ModelTask.Fast => FastModelName ?? ModelName,
        ModelTask.Smart => SmartModelName ?? ModelName,
        _ => ModelName
    };

    /// <summary>
    /// Загружает конфигурацию из agent.json если файл существует,
    /// иначе возвращает конфигурацию по умолчанию.
    ///
    /// Фаза 5: конфигурация через файл agent.json.
    /// </summary>
    public static AgentConfig LoadFromFileOrDefault(string? configPath = null)
    {
        var searchPaths = new List<string>();

        if (!string.IsNullOrEmpty(configPath))
            searchPaths.Add(configPath);

        searchPaths.Add("agent.json");
        searchPaths.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet-agent", "agent.json"));

        foreach (var path in searchPaths)
        {
            if (!File.Exists(path)) continue;

            try
            {
                var json = File.ReadAllText(path);
                var config = System.Text.Json.JsonSerializer.Deserialize<AgentConfig>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config != null)
                    return config;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"⚠️  Ошибка загрузки {path}: {ex.Message}");
            }
        }

        return new AgentConfig();
    }
}

/// <summary>Тип задачи для выбора модели (Фаза 5).</summary>
public enum ModelTask
{
    /// <summary>Обычная задача — используется ModelName</summary>
    Default,
    /// <summary>Быстрый ответ — используется FastModelName</summary>
    Fast,
    /// <summary>Сложный анализ кода — используется SmartModelName</summary>
    Smart
}


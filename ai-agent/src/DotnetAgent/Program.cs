// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Точка входа в .NET AI Агент
//
// Здесь мы:
//   1. Читаем конфигурацию (путь до проекта, настройки модели)
//   2. Создаём все зависимости (HTTP клиент, реестр инструментов, агент)
//   3. Запускаем главный интерактивный цикл (или MCP сервер)
// ─────────────────────────────────────────────────────────────────────────────

using DotnetAgent.Config;
using DotnetAgent.Core;
using DotnetAgent.Mcp;
using DotnetAgent.Rag;
using DotnetAgent.Tools;

// ─── Режим MCP сервера (Фаза 4) ──────────────────────────────────────────────
// Если запущено с флагом --mcp — запускаем stdio MCP сервер
// и не выводим никакой UI в консоль
var isMcpMode = args.Contains("--mcp");

// ─── Отображение заголовка ────────────────────────────────────────────────────
if (!isMcpMode)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║        .NET AI Агент  v2.0.0             ║");
    Console.WriteLine("║  Локальный ассистент для .NET проектов   ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}

// ─── Загрузка конфигурации ────────────────────────────────────────────────────
// Пробуем загрузить из agent.json, иначе используем значения по умолчанию
var configFilePath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Split('=', 2)[1];
var config = AgentConfig.LoadFromFileOrDefault(configFilePath);

// Путь до проекта можно передать первым аргументом командной строки:
//   dotnet run -- "C:\Projects\MyApp"
// Или переменной окружения DOTNET_AGENT_PROJECT_PATH
var projectPathArg = args.FirstOrDefault(a => !a.StartsWith("--"));
if (!string.IsNullOrEmpty(projectPathArg))
{
    config.ProjectPath = projectPathArg;
}
else if (Environment.GetEnvironmentVariable("DOTNET_AGENT_PROJECT_PATH") is { } envPath && !string.IsNullOrEmpty(envPath))
{
    config.ProjectPath = envPath;
}
else if (!isMcpMode)
{
    // Если путь не передан — спрашиваем у пользователя интерактивно
    Console.Write("📁 Введите путь до .NET проекта: ");
    config.ProjectPath = (Console.ReadLine() ?? "").Trim().Trim('"');
}

// Проверяем что директория существует
if (string.IsNullOrEmpty(config.ProjectPath) || !Directory.Exists(config.ProjectPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Ошибка: директория не найдена: '{config.ProjectPath}'");
    Console.ResetColor();
    Console.WriteLine("Пример: dotnet run -- \"C:\\Projects\\MyApp\"");
    return 1; // Код выхода 1 = ошибка
}

// Нормализуем путь
config.ProjectPath = Path.GetFullPath(config.ProjectPath);

if (!isMcpMode)
{
    Console.WriteLine($"📁 Проект:    {config.ProjectPath}");
    Console.WriteLine($"🤖 Модель:    {config.ModelName}");
    Console.WriteLine($"🌐 Ollama:    {config.OllamaUrl}");
    Console.WriteLine($"💾 Сессии:    {(config.EnableSessionPersistence ? "включены" : "отключены")}");
    Console.WriteLine($"📡 Streaming: {(config.EnableStreaming ? "включён" : "отключён")}");
    Console.WriteLine();
}

// ─── Создание зависимостей ────────────────────────────────────────────────────

// HTTP клиент для Ollama API
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5)
};

// Клиент Ollama
var ollamaClient = new OllamaClient(httpClient, config.OllamaUrl, config.ModelName);

// Реестр инструментов
var toolRegistry = new ToolRegistry();

// ── Фаза 1: инструменты файловой системы ──────────────────────────────────────
toolRegistry.RegisterMany(FileSystemTools.Create(config.ProjectPath));

// ── Фаза 2: Roslyn (анализ C# кода) ───────────────────────────────────────────
toolRegistry.RegisterMany(RoslynTools.Create(config.ProjectPath));

// ── Фаза 2: dotnet build / dotnet test ────────────────────────────────────────
toolRegistry.RegisterMany(BuildTools.Create(config.ProjectPath));

// ── Фаза 4: Git интеграция ─────────────────────────────────────────────────────
var gitTools = GitTools.Create(config.ProjectPath).ToList();
toolRegistry.RegisterMany(gitTools);

// ── Фаза 5: генерация тестов ───────────────────────────────────────────────────
toolRegistry.RegisterMany(TestGenerationTools.Create(config.ProjectPath));

// ── Фаза 5: RAG (ChromaDB) — регистрируем если ChromaDB доступен ───────────────
// Создаём ChromaClient и проверяем доступность ChromaDB
var chromaHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
var chromaClient = new ChromaClient(chromaHttpClient, config.ChromaDbUrl);
if (await chromaClient.IsAvailableAsync())
{
    toolRegistry.RegisterMany(RagTools.Create(
        config.ProjectPath, chromaClient, config.OllamaUrl, config.EmbeddingModel));
    if (!isMcpMode)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ ChromaDB доступен — RAG инструменты активированы");
        Console.ResetColor();
        Console.WriteLine();
    }
}

// ─── Запуск ────────────────────────────────────────────────────────────────────

// Фаза 4: MCP режим — запускаем stdio MCP сервер
if (isMcpMode)
{
    var mcpServer = new McpServer(toolRegistry);
    await mcpServer.RunAsync();
    return 0;
}

// Обычный консольный режим
var agent = new Agent(ollamaClient, toolRegistry, config);

Console.WriteLine("Введите задачу или 'помощь' для списка команд.");
Console.WriteLine("Примеры:");
Console.WriteLine("  > Покажи структуру проекта");
Console.WriteLine("  > Найди все контроллеры");
Console.WriteLine("  > Покажи структуру класса UserService");
Console.WriteLine("  > Запусти сборку проекта");
Console.WriteLine("  > Сгенерируй тесты для OrderService");
Console.WriteLine();

await agent.RunAsync();

return 0;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Точка входа в .NET AI Агент
//
// Здесь мы:
//   1. Читаем конфигурацию (путь до проекта, настройки модели)
//   2. Создаём все зависимости (HTTP клиент, реестр инструментов, агент)
//   3. Запускаем главный интерактивный цикл
// ─────────────────────────────────────────────────────────────────────────────

using DotnetAgent.Config;
using DotnetAgent.Core;
using DotnetAgent.Tools;

// ─── Отображение заголовка ────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║        .NET AI Агент  v0.1.0             ║");
Console.WriteLine("║  Локальный ассистент для .NET проектов   ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ─── Загрузка конфигурации ────────────────────────────────────────────────────
// Конфигурацию заполняем значениями по умолчанию.
// В будущем можно загружать из appsettings.json или переменных окружения.
var config = new AgentConfig();

// Путь до проекта можно передать первым аргументом командной строки:
//   dotnet run -- "C:\Projects\MyApp"
// Или переменной окружения DOTNET_AGENT_PROJECT_PATH
if (args.Length > 0)
{
    config.ProjectPath = args[0];
}
else if (Environment.GetEnvironmentVariable("DOTNET_AGENT_PROJECT_PATH") is { } envPath && !string.IsNullOrEmpty(envPath))
{
    config.ProjectPath = envPath;
}
else
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

// Нормализуем путь (убираем trailing slash и т.п.)
config.ProjectPath = Path.GetFullPath(config.ProjectPath);

// Показываем итоговую конфигурацию
Console.WriteLine($"📁 Проект:    {config.ProjectPath}");
Console.WriteLine($"🤖 Модель:    {config.ModelName}");
Console.WriteLine($"🌐 Ollama:    {config.OllamaUrl}");
Console.WriteLine();

// ─── Создание зависимостей (Dependency Injection вручную) ────────────────────
// В production приложении лучше использовать Microsoft.Extensions.DependencyInjection,
// но для простоты стартера создаём зависимости вручную.

// HTTP клиент для запросов к Ollama API
// Таймаут 5 минут — некоторые запросы к LLM могут занимать время
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5)
};

// Клиент для Ollama API (обёртка над HttpClient)
var ollamaClient = new OllamaClient(httpClient, config.OllamaUrl, config.ModelName);

// Реестр инструментов — агент будет использовать их для работы с проектом
var toolRegistry = new ToolRegistry();

// Регистрируем инструменты для работы с файловой системой:
// list_files, read_file, write_file, create_file, search_in_files
toolRegistry.RegisterMany(FileSystemTools.Create(config.ProjectPath));

// Главный агент — объединяет LLM и инструменты
var agent = new Agent(ollamaClient, toolRegistry, config);

// ─── Запуск ────────────────────────────────────────────────────────────────────
Console.WriteLine("Введите задачу или 'выход' для завершения.");
Console.WriteLine("Примеры:");
Console.WriteLine("  > Покажи структуру проекта");
Console.WriteLine("  > Найди все контроллеры в проекте");
Console.WriteLine("  > Прочитай файл Program.cs");
Console.WriteLine("  > Добавь логирование в UserService");
Console.WriteLine();

await agent.RunAsync();

return 0; // Код выхода 0 = успех

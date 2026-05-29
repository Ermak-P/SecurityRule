using System.Text.Json;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для сохранения и загрузки контекста проекта.
///
/// Контекст — это текстовый файл (.agent-context.md) в корне проекта,
/// где агент хранит своё "знание" о проекте: технологии, архитектуру,
/// ключевые классы и т.п.
///
/// Это позволяет агенту не сканировать проект заново при каждом запуске:
///   1. Один раз: "Проанализируй проект и сохрани контекст"
///   2. При следующем запуске контекст загружается автоматически
///   3. При обновлении проекта: "Обнови контекст проекта"
/// </summary>
public static class ContextTools
{
    /// <summary>
    /// Имя файла контекста в директории проекта.
    /// Рекомендуется добавить в .gitignore.
    /// </summary>
    public const string ContextFileName = ".agent-context.md";

    /// <summary>
    /// Возвращает полный путь к файлу контекста для данного проекта.
    /// </summary>
    public static string GetContextFilePath(string projectPath) =>
        Path.Combine(projectPath, ContextFileName);

    /// <summary>
    /// Создаёт все инструменты контекста.
    /// </summary>
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        yield return new SaveProjectContextTool(projectPath);
        yield return new GetProjectContextTool(projectPath);
    }
}

/// <summary>
/// Инструмент сохранения контекста проекта в файл.
///
/// Агент вызывает этот инструмент после анализа проекта, передавая
/// подробное описание: технологии, архитектуру, ключевые классы,
/// структуру директорий, правила проекта и т.п.
///
/// Файл сохраняется как .agent-context.md в корне проекта.
/// При следующем запуске агент автоматически загрузит этот файл.
/// </summary>
internal sealed class SaveProjectContextTool(string projectPath) : IAgentTool
{
    public string Name => "save_project_context";

    public string Description =>
        "Сохраняет описание/анализ проекта в файл .agent-context.md для ускорения будущих сессий. " +
        "Вызывай после полного анализа проекта, чтобы при следующем запуске агент сразу знал структуру. " +
        "Также используй для обновления контекста когда проект значительно изменился.";

    public object Parameters => new
    {
        type = "object",
        properties = new
        {
            context = new
            {
                type = "string",
                description =
                    "Полное описание проекта: технологии, архитектура, ключевые проекты/классы, " +
                    "зависимости, точки входа, соглашения по коду. " +
                    "Рекомендуемый объём: 20–100 строк."
            }
        },
        required = new[] { "context" }
    };

    public Task<string> ExecuteAsync(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("context", out var contextEl) ||
            string.IsNullOrWhiteSpace(contextEl.GetString()))
            return Task.FromResult("Ошибка: параметр 'context' обязателен и не может быть пустым.");

        var context = contextEl.GetString()!;
        var filePath = ContextTools.GetContextFilePath(projectPath);

        try
        {
            var header = $"# Контекст проекта\n\n" +
                         $"_Сгенерировано: {DateTime.Now:yyyy-MM-dd HH:mm}_\n\n";
            File.WriteAllText(filePath, header + context);
            return Task.FromResult(
                $"✓ Контекст сохранён в {filePath} ({context.Length} символов). " +
                "При следующем запуске агент загрузит его автоматически.");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Ошибка сохранения контекста: {ex.Message}");
        }
    }
}

/// <summary>
/// Инструмент чтения сохранённого контекста проекта.
///
/// Позволяет агенту посмотреть что уже записано в .agent-context.md,
/// прежде чем обновлять или дополнять его.
/// </summary>
internal sealed class GetProjectContextTool(string projectPath) : IAgentTool
{
    public string Name => "get_project_context";

    public string Description =>
        "Читает сохранённый контекст проекта из файла .agent-context.md. " +
        "Используй чтобы проверить текущий сохранённый контекст перед его обновлением.";

    public object Parameters => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public Task<string> ExecuteAsync(JsonElement arguments)
    {
        var filePath = ContextTools.GetContextFilePath(projectPath);

        if (!File.Exists(filePath))
            return Task.FromResult("Файл контекста (.agent-context.md) не найден. " +
                                   "Используй save_project_context чтобы создать его.");

        try
        {
            var content = File.ReadAllText(filePath);
            return Task.FromResult($"Содержимое {filePath}:\n\n{content}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Ошибка чтения контекста: {ex.Message}");
        }
    }
}

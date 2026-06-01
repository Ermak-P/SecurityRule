using System.Text.Json;
using DotnetAgent.Models;

namespace DotnetAgent.Tools;

/// <summary>
/// Реестр всех инструментов агента.
///
/// Это центральное место где хранятся и регистрируются все инструменты.
/// Агент использует реестр для:
///   1. Получения списка инструментов для передачи в LLM (GetToolDefinitions)
///   2. Выполнения инструмента когда LLM его запросил (GetTool + ExecuteAsync)
///
/// Пример использования:
/// <code>
///   var registry = new ToolRegistry();
///   registry.Register(new ReadFileTool(projectPath));
///   registry.RegisterMany(FileSystemTools.Create(projectPath));
///
///   // Получить определения для LLM
///   var definitions = registry.GetToolDefinitions();
///
///   // Выполнить инструмент
///   var tool = registry.GetTool("read_file");
///   var result = await tool.ExecuteAsync(arguments);
/// </code>
/// </summary>
public class ToolRegistry
{
    // Словарь: имя инструмента → экземпляр инструмента
    // Dictionary обеспечивает O(1) поиск по имени
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Регистрирует один инструмент.
    /// Если инструмент с таким именем уже зарегистрирован — перезаписывает его.
    /// </summary>
    public void Register(IAgentTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Регистрирует несколько инструментов сразу.
    /// Удобно для регистрации целых групп инструментов.
    ///
    /// Пример: registry.RegisterMany(FileSystemTools.Create(projectPath));
    /// </summary>
    public void RegisterMany(IEnumerable<IAgentTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        foreach (var tool in tools)
            Register(tool);
    }

    /// <summary>
    /// Возвращает инструмент по имени.
    /// Возвращает null если инструмент не найден (не зарегистрирован).
    /// </summary>
    public IAgentTool? GetTool(string name) =>
        _tools.TryGetValue(name, out var tool) ? tool : null;

    /// <summary>
    /// Возвращает все зарегистрированные инструменты.
    /// </summary>
    public IReadOnlyCollection<IAgentTool> GetAllTools() => _tools.Values;

    /// <summary>
    /// Возвращает определения всех инструментов в формате для Ollama API.
    ///
    /// Этот метод конвертирует наши IAgentTool в формат ToolDefinition
    /// который понимает Ollama. Результат передаётся в каждый запрос к LLM
    /// чтобы модель знала какие инструменты доступны.
    ///
    /// JSON Schema параметров (IAgentTool.Parameters) сериализуется через
    /// JsonSerializer — это позволяет описывать схему с помощью обычных C# объектов.
    /// </summary>
    public List<ToolDefinition> GetToolDefinitions()
    {
        var definitions = new List<ToolDefinition>(_tools.Count);

        foreach (var tool in _tools.Values)
        {
            // Сериализуем описание параметров в JSON, затем парсим обратно в JsonElement
            // Это нужно потому что FunctionDefinition.Parameters ожидает JsonElement
            var parametersJson = JsonSerializer.Serialize(tool.Parameters);
            var parametersElement = JsonDocument.Parse(parametersJson).RootElement.Clone();

            definitions.Add(new ToolDefinition
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = parametersElement
                }
            });
        }

        return definitions;
    }
}

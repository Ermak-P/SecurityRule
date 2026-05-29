using System.Text;
using DotnetAgent.Config;
using DotnetAgent.Models;
using DotnetAgent.Tools;

namespace DotnetAgent.Core;

/// <summary>
/// Главный класс агента — реализует цикл "думать → действовать → думать".
///
/// ══════════════════════════════════════════════════════════
/// АРХИТЕКТУРА: паттерн ReAct (Reasoning + Acting)
/// ══════════════════════════════════════════════════════════
///
/// ReAct — это основной подход в построении AI агентов.
/// Агент работает в цикле:
///
///   Пользователь: "Найди все TODO в проекте"
///       ↓
///   [THINK] Отправить запрос в LLM: "что нужно сделать?"
///       ↓
///   LLM: "Мне нужно использовать search_in_files с query='TODO'"
///       ↓
///   [ACT] Выполнить инструмент search_in_files
///       ↓
///   Результат: "TODO в файлах: Program.cs:15, UserService.cs:42..."
///       ↓
///   [THINK] Отправить результат в LLM: "что теперь?"
///       ↓
///   LLM: "Вот список TODO комментариев: ..." (финальный ответ)
///       ↓
///   Пользователь видит ответ
///
/// Цикл продолжается пока LLM вызывает инструменты.
/// Когда LLM даёт текстовый ответ без tool_calls — цикл завершается.
/// ══════════════════════════════════════════════════════════
/// </summary>
public class Agent
{
    private readonly OllamaClient _ollamaClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly AgentConfig _config;

    // История разговора.
    // Сохраняется между запросами внутри одной сессии (пока программа запущена).
    // Позволяет LLM помнить контекст предыдущих сообщений.
    //
    // Формат: [system, user, assistant, tool, assistant, user, assistant, ...]
    private readonly List<ChatMessage> _conversationHistory = new();

    public Agent(OllamaClient ollamaClient, ToolRegistry toolRegistry, AgentConfig config)
    {
        _ollamaClient = ollamaClient;
        _toolRegistry = toolRegistry;
        _config = config;
    }

    /// <summary>
    /// Запускает интерактивный консольный цикл.
    ///
    /// Читает ввод пользователя, запускает агента, выводит ответ.
    /// Повторяет пока пользователь не введёт "выход".
    /// </summary>
    public async Task RunAsync()
    {
        // ── Проверка соединения с Ollama ──────────────────────────────────────
        Console.Write("Проверяю соединение с Ollama...");
        if (!await _ollamaClient.IsAvailableAsync())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ✗ НЕДОСТУПЕН");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"Ollama не отвечает по адресу: {_config.OllamaUrl}");
            Console.WriteLine();
            Console.WriteLine("Для запуска Ollama в Docker:");
            Console.WriteLine("  1. cd ai-agent");
            Console.WriteLine("  2. docker compose up -d");
            Console.WriteLine($"  3. docker exec -it ollama ollama pull {_config.ModelName}");
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ✓ OK");
        Console.ResetColor();

        // Показываем доступные модели как дополнительную диагностику
        var models = await _ollamaClient.GetAvailableModelsAsync();
        if (models.Length > 0)
        {
            Console.WriteLine($"Доступные модели: {string.Join(", ", models)}");
            if (!models.Any(m => m.StartsWith(_config.ModelName.Split(':')[0], StringComparison.OrdinalIgnoreCase)))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  Модель '{_config.ModelName}' не найдена!");
                Console.WriteLine($"   Скачайте её: docker exec -it ollama ollama pull {_config.ModelName}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();

        // ── Инициализация системного промпта ──────────────────────────────────
        // Системный промпт добавляется один раз в начале разговора.
        // Он задаёт "личность" и правила поведения LLM.
        _conversationHistory.Add(BuildSystemMessage());

        // Кешируем список инструментов (не меняется в ходе работы)
        var toolDefinitions = _toolRegistry.GetToolDefinitions();

        // ── Главный цикл ──────────────────────────────────────────────────────
        while (true)
        {
            // Читаем ввод пользователя
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\n> ");
            Console.ResetColor();

            var userInput = Console.ReadLine()?.Trim() ?? "";

            // Обрабатываем служебные команды
            if (string.IsNullOrEmpty(userInput)) continue;

            switch (userInput.ToLowerInvariant())
            {
                case "выход" or "exit" or "quit" or "q":
                    Console.WriteLine("До свидания!");
                    return;

                case "история":
                    PrintConversationHistory();
                    continue;

                case "очистить" or "clear":
                    // Очищаем историю но оставляем системный промпт
                    _conversationHistory.RemoveAll(m => m.Role != "system");
                    Console.WriteLine("✓ История разговора очищена.");
                    continue;

                case "инструменты" or "tools":
                    PrintAvailableTools();
                    continue;

                case "помощь" or "help":
                    PrintHelp();
                    continue;
            }

            // Добавляем сообщение пользователя в историю
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userInput });

            // Запускаем цикл агента
            await RunAgentLoopAsync(toolDefinitions);
        }
    }

    /// <summary>
    /// Внутренний цикл агента: LLM ↔ Инструменты.
    ///
    /// Отправляет текущую историю в LLM.
    /// Если LLM возвращает tool_calls — выполняет инструменты и повторяет.
    /// Если LLM возвращает текст — выводит его пользователю и завершает цикл.
    ///
    /// Максимум MaxToolCallsPerRequest итераций для защиты от бесконечного цикла.
    /// </summary>
    private async Task RunAgentLoopAsync(List<ToolDefinition> toolDefinitions)
    {
        var toolCallCount = 0;

        while (true)
        {
            // Индикатор что агент думает
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("🤔 Думаю...");
            Console.ResetColor();

            // ── Запрос к LLM ──────────────────────────────────────────────────
            ChatResponse response;
            try
            {
                response = await _ollamaClient.ChatAsync(
                    _conversationHistory,
                    toolDefinitions,
                    _config.Temperature,
                    _config.ContextWindowSize);
            }
            catch (Exception ex)
            {
                // Очищаем строку "Думаю..."
                Console.Write("\r                    \r");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.ResetColor();

                // Удаляем последнее сообщение пользователя из истории
                // чтобы пользователь мог повторить запрос
                if (_conversationHistory.Count > 0 && _conversationHistory[^1].Role == "user")
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

                return;
            }

            // Очищаем строку "Думаю..."
            Console.Write("\r                    \r");

            // Проверяем корректность ответа
            if (response.Message == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ LLM вернул пустой ответ");
                Console.ResetColor();
                return;
            }

            // Добавляем ответ LLM в историю разговора
            _conversationHistory.Add(response.Message);

            // ── Обработка вызовов инструментов ────────────────────────────────
            if (response.Message.ToolCalls is { Count: > 0 } toolCalls)
            {
                // Защита от бесконечного цикла
                if (toolCallCount >= _config.MaxToolCallsPerRequest)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  Достигнут лимит вызовов инструментов ({_config.MaxToolCallsPerRequest}).");
                    Console.WriteLine("Увеличьте MaxToolCallsPerRequest в AgentConfig если нужно.");
                    Console.ResetColor();
                    break;
                }

                // Выполняем каждый запрошенный инструмент
                foreach (var toolCall in toolCalls)
                {
                    toolCallCount++;
                    var toolName = toolCall.Function.Name;

                    // Показываем какой инструмент вызывается
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($"  🔧 {toolName}");
                    Console.ResetColor();

                    // Ищем и выполняем инструмент
                    var tool = _toolRegistry.GetTool(toolName);
                    string toolResult;

                    if (tool == null)
                    {
                        toolResult = $"Ошибка: инструмент '{toolName}' не найден";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" → не найден");
                        Console.ResetColor();
                    }
                    else
                    {
                        try
                        {
                            toolResult = await tool.ExecuteAsync(toolCall.Function.Arguments);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($" → {toolResult.Length} симв.");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            toolResult = $"Ошибка выполнения инструмента '{toolName}': {ex.Message}";
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($" → ошибка: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                    // Добавляем результат в историю как сообщение role="tool"
                    // Ollama передаст этот результат обратно в LLM на следующей итерации
                    _conversationHistory.Add(new ChatMessage
                    {
                        Role = "tool",
                        Content = toolResult
                    });
                }

                // Продолжаем цикл — LLM получит результаты инструментов и продолжит работу
                continue;
            }

            // ── Финальный ответ LLM ────────────────────────────────────────────
            // Если LLM не вызвал инструменты — он дал финальный ответ
            if (!string.IsNullOrEmpty(response.Message.Content))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("── Ответ ──────────────────────────────────────────────────");
                Console.ResetColor();
                Console.WriteLine(response.Message.Content);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("───────────────────────────────────────────────────────────");
                Console.ResetColor();
            }

            // Выходим из цикла агента
            break;
        }
    }

    /// <summary>
    /// Создаёт системный промпт для LLM.
    ///
    /// Системный промпт — это инструкции которые задают "личность" LLM:
    ///   - Кто он такой (агент для .NET проектов)
    ///   - Что он умеет (какие инструменты использовать)
    ///   - Какие правила соблюдать
    ///   - На каком языке отвечать
    ///
    /// Качество системного промпта напрямую влияет на полезность агента!
    /// </summary>
    private ChatMessage BuildSystemMessage()
    {
        // Строим список доступных инструментов для включения в промпт
        var toolsList = new StringBuilder();
        foreach (var tool in _toolRegistry.GetAllTools())
            toolsList.AppendLine($"  - {tool.Name}: {tool.Description.Split('.')[0]}");

        var systemPrompt = $"""
            Ты — AI агент специализирующийся на анализе и изменении .NET/C# проектов.
            Твоя цель — помогать разработчику понимать и улучшать код.
            
            АНАЛИЗИРУЕМЫЙ ПРОЕКТ: {_config.ProjectPath}
            
            ДОСТУПНЫЕ ИНСТРУМЕНТЫ:
            {toolsList}
            
            КАК РАБОТАТЬ:
            1. Начинай с list_files чтобы понять структуру проекта
            2. Используй read_file для чтения файлов ПЕРЕД любыми изменениями
            3. Используй search_in_files для поиска классов, методов, паттернов
            4. При изменении файлов: read_file → изменить → write_file (с ПОЛНЫМ содержимым)
            5. Объясняй что ты делаешь и почему
            
            ПРАВИЛА:
            - Всегда читай файл перед изменением
            - Сохраняй стиль кода существующего проекта
            - Отвечай на русском языке
            - Работай только с файлами внутри директории проекта
            - Если задача неясна — уточни у пользователя
            - Сообщай об ошибках понятным языком
            """;

        return new ChatMessage { Role = "system", Content = systemPrompt };
    }

    /// <summary>Выводит краткую историю разговора</summary>
    private void PrintConversationHistory()
    {
        Console.WriteLine("\n─── История разговора ─────────────────────────────────");
        int msgNum = 0;
        foreach (var msg in _conversationHistory)
        {
            if (msg.Role == "system") continue;

            Console.ForegroundColor = msg.Role switch
            {
                "user" => ConsoleColor.Yellow,
                "assistant" => ConsoleColor.Cyan,
                "tool" => ConsoleColor.DarkGray,
                _ => ConsoleColor.White
            };

            var preview = msg.Content?.Replace('\n', ' ') ?? "(tool call)";
            if (preview.Length > 120) preview = preview[..120] + "...";

            Console.WriteLine($"  [{++msgNum}] {msg.Role}: {preview}");
        }
        Console.ResetColor();
        Console.WriteLine("───────────────────────────────────────────────────────");
    }

    /// <summary>Выводит список доступных инструментов</summary>
    private void PrintAvailableTools()
    {
        Console.WriteLine("\n─── Доступные инструменты ──────────────────────────────");
        foreach (var tool in _toolRegistry.GetAllTools())
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  🔧 {tool.Name}");
            Console.ResetColor();
            Console.WriteLine($": {tool.Description.Split('.')[0]}");
        }
        Console.WriteLine("───────────────────────────────────────────────────────");
    }

    /// <summary>Выводит помощь</summary>
    private static void PrintHelp()
    {
        Console.WriteLine("""

        ─── Справка ────────────────────────────────────────────
          Введите любой текст — агент выполнит задачу.
          
          Примеры задач:
            > Покажи структуру проекта
            > Найди все классы наследующие ControllerBase
            > Прочитай файл Program.cs
            > Найди все TODO комментарии
            > Добавь XML документацию к методу GetUsers
            > Создай новый сервис EmailService с интерфейсом IEmailService
          
          Служебные команды:
            история     — показать историю разговора
            очистить    — очистить историю (новый контекст)
            инструменты — список доступных инструментов
            помощь      — эта справка
            выход       — завершить работу
        ───────────────────────────────────────────────────────
        """);
    }
}

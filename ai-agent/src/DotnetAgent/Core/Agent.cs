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
///
/// Фаза 3: streaming ответов, сохранение сессий, planning mode, undo.
/// </summary>
public class Agent
{
    private readonly OllamaClient _ollamaClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly AgentConfig _config;
    private readonly UndoManager _undoManager = new();

    // История разговора (в рамках сессии)
    private readonly List<ChatMessage> _conversationHistory = new();

    // Фаза 3: хранилище сессий (может быть null если persistence отключена)
    private SessionStore? _sessionStore;
    private long _currentSessionId;

    /// <summary>
    /// Фиксированный промпт для команды "обнови контекст".
    /// Пользователь просто вводит команду — агент получает этот текст автоматически.
    /// </summary>
    private const string UpdateContextPrompt =
        "Проанализируй проект полностью: технологии, фреймворки, NuGet-пакеты, " +
        "структуру директорий и проектов в солюшене, назначение каждого проекта, " +
        "ключевые классы и интерфейсы, точки входа, паттерны и архитектурные решения, " +
        "соглашения по именованию и коду. " +
        "Собери всё это в структурированное описание и сохрани с помощью save_project_context.";

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

        // ── Фаза 3: загрузка сессии ────────────────────────────────────────────
        if (_config.EnableSessionPersistence)
            await InitializeSessionAsync();

        // ── Инициализация системного промпта ──────────────────────────────────
        // Системный промпт добавляется один раз в начале разговора.
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
                    _conversationHistory.RemoveAll(m => m.Role != "system");
                    Console.WriteLine("✓ История разговора очищена.");
                    continue;

                case "инструменты" or "tools":
                    PrintAvailableTools();
                    continue;

                case "помощь" or "help":
                    PrintHelp();
                    continue;

                // Фаза 3: откат последнего изменения
                case "undo" or "отменить" or "откат":
                    var undoResult = _undoManager.Undo();
                    if (undoResult == null)
                        Console.WriteLine("Нечего отменять.");
                    else
                        Console.WriteLine(undoResult);
                    continue;

                // Фаза 3: список последних сессий
                case "сессии" or "sessions":
                    PrintSessions();
                    continue;

                // Обновление контекста проекта одной командой
                case "обнови контекст" or "update context" or "обновить контекст":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Запускаю анализ проекта и обновление контекста...");
                    Console.ResetColor();
                    userInput = UpdateContextPrompt;
                    break;

                // Статус контекста проекта
                case "контекст" or "context":
                    PrintContextStatus();
                    continue;
            }

            // Добавляем сообщение пользователя в историю
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userInput });

            // Фаза 3: сохраняем в БД
            if (_sessionStore != null)
                _sessionStore.SaveMessage(_currentSessionId,
                    _conversationHistory[^1]);

            // Запускаем цикл агента
            await RunAgentLoopAsync(toolDefinitions);
        }
    }

    /// <summary>
    /// Фаза 3: инициализация или восстановление сессии.
    /// </summary>
    private async Task InitializeSessionAsync()
    {
        try
        {
            _sessionStore = SessionStore.CreateDefault();
            var lastSession = _sessionStore.GetLastSession(_config.ProjectPath);

            if (lastSession != null)
            {
                Console.WriteLine($"Найдена предыдущая сессия: {lastSession.Name} ({lastSession.CreatedAt})");
                Console.Write("Продолжить? (д/н): ");
                var answer = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";

                if (answer is "д" or "y" or "yes" or "да")
                {
                    _currentSessionId = lastSession.Id;
                    var history = _sessionStore.LoadMessages(_currentSessionId);
                    _conversationHistory.AddRange(history);
                    Console.WriteLine($"✓ Загружено {history.Count} сообщений из предыдущей сессии.");
                    Console.WriteLine();
                    return;
                }
            }

            // Создаём новую сессию
            var sessionName = $"Сессия {DateTime.Now:yyyy-MM-dd HH:mm}";
            _currentSessionId = _sessionStore.CreateSession(_config.ProjectPath, sessionName);
            Console.WriteLine($"✓ Новая сессия создана: {sessionName}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Сессии недоступны: {ex.Message}");
            Console.ResetColor();
            _sessionStore = null;
        }
    }

    /// <summary>
    /// Внутренний цикл агента: LLM ↔ Инструменты.
    ///
    /// Отправляет текущую историю в LLM.
    /// Если LLM возвращает tool_calls — выполняет инструменты и повторяет.
    /// Если LLM возвращает текст — выводит его пользователю и завершает цикл.
    ///
    /// Фаза 3: для финального ответа используется streaming.
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
                Console.Write("\r                    \r");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.ResetColor();

                // Удаляем последнее сообщение пользователя чтобы он мог повторить запрос
                if (_conversationHistory.Count > 0 && _conversationHistory[^1].Role == "user")
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

                return;
            }

            Console.Write("\r                    \r");

            if (response.Message == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ LLM вернул пустой ответ");
                Console.ResetColor();
                return;
            }

            // Добавляем ответ LLM в историю разговора
            _conversationHistory.Add(response.Message);
            if (_sessionStore != null)
                _sessionStore.SaveMessage(_currentSessionId, response.Message);

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

                foreach (var toolCall in toolCalls)
                {
                    toolCallCount++;
                    var toolName = toolCall.Function.Name;

                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($"  🔧 {toolName}");
                    Console.ResetColor();

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
                            // Фаза 3: backup для undo перед инструментами изменяющими файлы
                            using var tx = _undoManager.BeginTransaction(toolName);
                            BackupToolFiles(tx, toolName, toolCall.Function.Arguments);

                            toolResult = await tool.ExecuteAsync(toolCall.Function.Arguments);

                            // Фиксируем undo только если инструмент что-то изменил
                            if (IsWritingTool(toolName))
                                tx.Commit();

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

                    var toolMessage = new ChatMessage { Role = "tool", Content = toolResult };
                    _conversationHistory.Add(toolMessage);
                    if (_sessionStore != null)
                        _sessionStore.SaveMessage(_currentSessionId, toolMessage);
                }

                // Продолжаем цикл — LLM получит результаты и продолжит работу
                continue;
            }

            // ── Финальный ответ LLM ────────────────────────────────────────────
            if (!string.IsNullOrEmpty(response.Message.Content))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("── Ответ ──────────────────────────────────────────────────");
                Console.ResetColor();

                // Фаза 3: streaming для финального ответа (пересказываем через stream API)
                if (_config.EnableStreaming)
                {
                    await StreamFinalResponseAsync(toolDefinitions);
                    return; // streaming сам добавляет сообщение в историю
                }
                else
                {
                    Console.WriteLine(response.Message.Content);
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("───────────────────────────────────────────────────────────");
                Console.ResetColor();
            }

            break;
        }
    }

    /// <summary>
    /// Фаза 3: streaming финального ответа от LLM.
    ///
    /// Заменяем последнее assistant-сообщение в истории на "streaming-заготовку"
    /// и добавляем streaming ответ, выводя токены по мере поступления.
    /// </summary>
    private async Task StreamFinalResponseAsync(List<ToolDefinition> toolDefinitions)
    {
        // Убираем последнее assistant-сообщение (оно содержит ответ без streaming)
        // и получаем streaming ответ через отдельный запрос
        if (_conversationHistory.Count > 0 && _conversationHistory[^1].Role == "assistant")
            _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

        // Также убираем его из БД (будет добавлен заново со streaming содержимым)
        // Простой способ: просто получим streaming версию

        var fullResponse = new StringBuilder();

        try
        {
            await foreach (var token in _ollamaClient.ChatStreamAsync(
                _conversationHistory, toolDefinitions,
                _config.Temperature, _config.ContextWindowSize))
            {
                Console.Write(token);
                fullResponse.Append(token);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Ошибка streaming: {ex.Message}");
            Console.ResetColor();
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.ResetColor();

        // Сохраняем полный ответ в историю
        var assistantMessage = new ChatMessage
        {
            Role = "assistant",
            Content = fullResponse.ToString()
        };
        _conversationHistory.Add(assistantMessage);
        if (_sessionStore != null)
            _sessionStore.SaveMessage(_currentSessionId, assistantMessage);
    }

    /// <summary>
    /// Фаза 3: перед записью файла сохраняем backup для undo.
    /// </summary>
    private static void BackupToolFiles(UndoTransaction tx, string toolName, System.Text.Json.JsonElement args)
    {
        if (!IsWritingTool(toolName)) return;

        if (args.TryGetProperty("path", out var pathEl))
            tx.BackupFile(pathEl.GetString() ?? "");
        else if (args.TryGetProperty("file_path", out var filePathEl))
            tx.BackupFile(filePathEl.GetString() ?? "");
    }

    private static bool IsWritingTool(string toolName) =>
        toolName is "write_file" or "create_file" or "patch_method" or "git_commit";

    /// <summary>
    /// Создаёт системный промпт для LLM.
    ///
    /// Системный промпт задаёт "личность" LLM:
    ///   - Кто он такой (агент для .NET проектов)
    ///   - Какие инструменты доступны
    ///   - Какие правила соблюдать
    ///   - На каком языке отвечать
    /// </summary>
    private ChatMessage BuildSystemMessage()
    {
        var toolsList = new StringBuilder();
        foreach (var tool in _toolRegistry.GetAllTools())
            toolsList.AppendLine($"  - {tool.Name}: {tool.Description.Split('.').FirstOrDefault() ?? tool.Description}");

        var projectContextSection = !string.IsNullOrWhiteSpace(_config.ProjectContext)
            ? $"\nКОНТЕКСТ ПРОЕКТА:\n{_config.ProjectContext}\n"
            : "";

        var sessionContinuationHint = _conversationHistory.Count > 0
            ? "\nЭто продолжение существующей сессии. Структура проекта уже известна — не вызывай list_files повторно если пользователь не просит.\n"
            : "";

        var systemPromptExtra = !string.IsNullOrWhiteSpace(_config.SystemPromptExtra)
            ? $"\n{_config.SystemPromptExtra}"
            : "";

        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("Ты — AI агент специализирующийся на анализе и изменении .NET/C# проектов.");
        systemPrompt.AppendLine("Твоя цель — помогать разработчику понимать и улучшать код.");
        systemPrompt.AppendLine();
        systemPrompt.AppendLine($"АНАЛИЗИРУЕМЫЙ ПРОЕКТ: {_config.ProjectPath}");
        if (!string.IsNullOrWhiteSpace(projectContextSection))
            systemPrompt.Append(projectContextSection);
        systemPrompt.AppendLine();
        systemPrompt.AppendLine("ДОСТУПНЫЕ ИНСТРУМЕНТЫ:");
        systemPrompt.Append(toolsList);
        systemPrompt.AppendLine("КАК РАБОТАТЬ:");
        systemPrompt.AppendLine("1. Начинай с list_files чтобы понять структуру проекта");
        systemPrompt.AppendLine("2. Используй read_file для чтения файлов ПЕРЕД любыми изменениями");
        systemPrompt.AppendLine("3. Используй get_class_info для анализа структуры C# классов");
        systemPrompt.AppendLine("4. Используй search_in_files для поиска классов, методов, паттернов");
        systemPrompt.AppendLine("5. Используй patch_method для изменения одного метода (лучше чем write_file для больших файлов)");
        systemPrompt.AppendLine("6. Используй dotnet_build после изменений чтобы убедиться что код компилируется");
        systemPrompt.AppendLine("7. Объясняй что ты делаешь и почему");
        systemPrompt.AppendLine();
        systemPrompt.AppendLine("ПРАВИЛА:");
        systemPrompt.AppendLine("- Всегда читай файл перед изменением");
        systemPrompt.AppendLine("- Сохраняй стиль кода существующего проекта");
        systemPrompt.AppendLine("- Отвечай на русском языке");
        systemPrompt.AppendLine("- Работай только с файлами внутри директории проекта");
        systemPrompt.AppendLine("- Если задача неясна — уточни у пользователя");
        systemPrompt.AppendLine("- Сообщай об ошибках понятным языком");
        if (!string.IsNullOrWhiteSpace(sessionContinuationHint))
            systemPrompt.Append(sessionContinuationHint);
        if (!string.IsNullOrWhiteSpace(systemPromptExtra))
            systemPrompt.Append(systemPromptExtra);

        return new ChatMessage { Role = "system", Content = systemPrompt.ToString() };
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

    /// <summary>Фаза 3: выводит список сессий для текущего проекта</summary>
    private void PrintSessions()
    {
        if (_sessionStore == null)
        {
            Console.WriteLine("Сессии отключены (EnableSessionPersistence = false).");
            return;
        }

        var sessions = _sessionStore.ListSessions(_config.ProjectPath);
        if (sessions.Count == 0)
        {
            Console.WriteLine("Нет сохранённых сессий.");
            return;
        }

        Console.WriteLine("\n─── Последние сессии ───────────────────────────────────");
        foreach (var s in sessions)
            Console.WriteLine($"  [{s.Id}] {s.Name} ({s.CreatedAt})");
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
            Console.WriteLine($": {tool.Description.Split('.').FirstOrDefault() ?? tool.Description}");
        }
        Console.WriteLine("───────────────────────────────────────────────────────");
    }

    /// <summary>Выводит помощь</summary>
    private void PrintHelp()
    {
        Console.WriteLine("""

        ─── Справка ────────────────────────────────────────────
          Введите любой текст — агент выполнит задачу.
          
          Примеры задач:
            > Покажи структуру проекта
            > Найди все классы наследующие ControllerBase
            > Покажи структуру класса UserService
            > Найди где используется метод GetById
            > Прочитай файл Program.cs
            > Найди все TODO комментарии
            > Добавь XML документацию к методу GetUsers
            > Создай новый сервис EmailService с интерфейсом IEmailService
            > Запусти сборку проекта
            > Сгенерируй тесты для класса OrderService
          
          Контекст проекта (ускоряет работу в больших солюшенах):
            обнови контекст — проанализировать проект и сохранить контекст
            контекст        — показать статус сохранённого контекста
          
          Служебные команды:
            история     — показать историю разговора
            очистить    — очистить историю (новый контекст)
            инструменты — список доступных инструментов
            сессии      — список последних сессий
            undo        — отменить последнее изменение файла
            помощь      — эта справка
            выход       — завершить работу
        ───────────────────────────────────────────────────────
        """);
    }

    /// <summary>Выводит статус сохранённого контекста проекта</summary>
    private void PrintContextStatus()
    {
        var contextFile = Tools.ContextTools.GetContextFilePath(_config.ProjectPath);
        Console.WriteLine("\n─── Контекст проекта ───────────────────────────────────");
        if (File.Exists(contextFile))
        {
            var info = new FileInfo(contextFile);
            var size = info.Length;
            var modified = info.LastWriteTime;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Контекст загружен: {contextFile}");
            Console.ResetColor();
            Console.WriteLine($"  Размер:   {size:N0} байт");
            Console.WriteLine($"  Обновлён: {modified:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
            Console.WriteLine("  Чтобы обновить — введите: обнови контекст");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠  Контекст не сохранён.");
            Console.ResetColor();
            Console.WriteLine($"  Файл не найден: {contextFile}");
            Console.WriteLine();
            Console.WriteLine("  Введите: обнови контекст");
            Console.WriteLine("  Агент проанализирует проект и сохранит описание.");
            Console.WriteLine("  При следующем запуске контекст загрузится автоматически.");
        }
        Console.WriteLine("───────────────────────────────────────────────────────");
    }
}

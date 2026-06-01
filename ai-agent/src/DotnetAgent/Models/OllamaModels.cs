using System.Text.Json.Serialization;

namespace DotnetAgent.Models;

// ─────────────────────────────────────────────────────────────────────────────
// OllamaModels.cs — Модели данных для Ollama Chat API
//
// Ollama предоставляет REST API совместимый с OpenAI API.
// Документация: https://github.com/ollama/ollama/blob/main/docs/api.md
//
// Основной эндпоинт: POST http://localhost:11434/api/chat
//
// Пример запроса:
// {
//   "model": "qwen2.5-coder:7b",
//   "messages": [
//     { "role": "system", "content": "Ты помощник..." },
//     { "role": "user",   "content": "Привет!" }
//   ],
//   "tools": [...],   ← список функций которые может вызвать LLM
//   "stream": false
// }
//
// Пример ответа:
// {
//   "model": "qwen2.5-coder:7b",
//   "message": {
//     "role": "assistant",
//     "content": "Привет! Чем могу помочь?",
//     "tool_calls": null   ← если LLM хочет вызвать инструмент — здесь будет вызов
//   },
//   "done": true
// }
// ─────────────────────────────────────────────────────────────────────────────

// ─── Запрос к Ollama Chat API ─────────────────────────────────────────────────

/// <summary>
/// Запрос к POST /api/chat
/// </summary>
public class ChatRequest
{
    /// <summary>Название модели (должна быть скачана через ollama pull)</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    /// <summary>История разговора: system + user + assistant сообщения</summary>
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Инструменты (функции) которые LLM может вызвать.
    /// Если null — агент работает без инструментов (обычный чат).
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolDefinition>? Tools { get; set; }

    /// <summary>
    /// false = ждать полный ответ (проще для начала)
    /// true  = стриминг по частям (лучше UX, но сложнее реализовать)
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    /// <summary>Параметры генерации (температура, размер контекста и т.д.)</summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GenerationOptions? Options { get; set; }
}

/// <summary>
/// Параметры генерации текста.
/// Влияют на качество и скорость ответов.
/// </summary>
public class GenerationOptions
{
    /// <summary>
    /// Температура (0.0–1.0): насколько "случайны" ответы.
    /// 0.0 = детерминированный, 0.1 = рекомендуется для кода, 0.7 = творческий
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.1f;

    /// <summary>
    /// Размер контекстного окна в токенах.
    /// 1 токен ≈ 4 символа. 8192 = ~32 KB текста.
    /// </summary>
    [JsonPropertyName("num_ctx")]
    public int NumCtx { get; set; } = 8192;
}

// ─── Сообщения ────────────────────────────────────────────────────────────────

/// <summary>
/// Одно сообщение в разговоре.
///
/// Роли:
///   "system"    — инструкции для LLM (как он должен себя вести)
///   "user"      — сообщение от пользователя
///   "assistant" — ответ LLM (может содержать tool_calls)
///   "tool"      — результат выполнения инструмента
/// </summary>
public class ChatMessage
{
    /// <summary>Роль отправителя: system / user / assistant / tool</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    /// <summary>
    /// Текстовое содержимое сообщения.
    /// Может быть null если сообщение содержит только tool_calls.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// Вызовы инструментов от LLM.
    /// Заполнено только в сообщениях с role="assistant" когда LLM хочет вызвать функцию.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCall>? ToolCalls { get; set; }
}

// ─── Tool Calling (вызов функций/инструментов) ────────────────────────────────
//
// Механизм "Tool Calling" позволяет LLM запрашивать выполнение внешних функций.
//
// Пример flow:
//   1. Мы передаём LLM список инструментов (ToolDefinition)
//   2. LLM решает: "нужен ли мне инструмент чтобы ответить?"
//   3. Если нужен — LLM возвращает ToolCall вместо текстового ответа
//   4. Мы выполняем инструмент и возвращаем результат в LLM как role="tool"
//   5. LLM генерирует финальный ответ с учётом результата
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Определение инструмента (функции) — передаётся в LLM в каждом запросе.
/// LLM читает описание и параметры чтобы понять когда и как использовать инструмент.
/// </summary>
public class ToolDefinition
{
    /// <summary>Тип инструмента. Всегда "function" для Ollama.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>Описание функции: имя, описание, параметры</summary>
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// Описание конкретной функции для LLM.
/// Чем точнее описание и параметры — тем правильнее LLM будет вызывать функцию.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Уникальное имя функции (используется LLM для вызова).
    /// Только латиница, цифры, подчёркивания. Например: "read_file", "list_files".
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Описание что делает функция.
    /// LLM опирается на это описание чтобы решить — вызывать функцию или нет.
    /// Чем подробнее — тем лучше!
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// JSON Schema параметров функции.
    /// Описывает какие аргументы принимает функция и их типы.
    ///
    /// Пример:
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "path": { "type": "string", "description": "Путь к файлу" }
    ///   },
    ///   "required": ["path"]
    /// }
    /// </summary>
    [JsonPropertyName("parameters")]
    public System.Text.Json.JsonElement Parameters { get; set; }
}

/// <summary>
/// Вызов инструмента от LLM.
/// LLM возвращает это когда хочет выполнить какую-то функцию.
/// </summary>
public class ToolCall
{
    /// <summary>Какую именно функцию вызвать и с какими аргументами</summary>
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

/// <summary>
/// Конкретный вызов функции с аргументами.
/// </summary>
public class FunctionCall
{
    /// <summary>Имя функции для вызова (должно совпадать с Name в ToolDefinition)</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Аргументы в формате JSON.
    /// Мы передаём их в IAgentTool.ExecuteAsync() для выполнения.
    /// </summary>
    [JsonPropertyName("arguments")]
    public System.Text.Json.JsonElement Arguments { get; set; }
}

// ─── Ответ от Ollama ──────────────────────────────────────────────────────────

/// <summary>
/// Ответ от POST /api/chat (при stream=false)
/// </summary>
public class ChatResponse
{
    /// <summary>Название модели которая генерировала ответ</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    /// <summary>
    /// Ответное сообщение.
    /// Если LLM хочет вызвать инструмент — message.ToolCalls будет заполнен.
    /// Если LLM даёт текстовый ответ — message.Content будет заполнен.
    /// </summary>
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }

    /// <summary>true = генерация завершена</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }

    /// <summary>
    /// Текст ошибки (если что-то пошло не так).
    /// Например: модель не найдена, закончилась память и т.д.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}

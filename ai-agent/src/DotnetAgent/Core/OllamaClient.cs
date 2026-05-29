using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DotnetAgent.Models;

namespace DotnetAgent.Core;

/// <summary>
/// HTTP клиент для работы с Ollama REST API.
///
/// Ollama — это локальный сервер для запуска языковых моделей (LLM).
/// Он запускается в Docker и предоставляет API совместимый с OpenAI.
///
/// Документация Ollama API:
///   https://github.com/ollama/ollama/blob/main/docs/api.md
///
/// Этот класс является тонкой обёрткой над HttpClient.
/// Он отвечает только за HTTP запросы — без бизнес-логики.
/// </summary>
public class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _modelName;

    // Настройки JSON сериализации/десериализации
    // PropertyNameCaseInsensitive=true — принимаем и camelCase и snake_case от Ollama
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <param name="httpClient">HTTP клиент (инжектируется снаружи для удобства тестирования)</param>
    /// <param name="baseUrl">Базовый URL Ollama, например http://localhost:11434</param>
    /// <param name="modelName">Название модели, например qwen2.5-coder:7b</param>
    public OllamaClient(HttpClient httpClient, string baseUrl, string modelName)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/'); // Убираем trailing slash
        _modelName = modelName;
    }

    /// <summary>
    /// Отправляет сообщения в Ollama Chat API и возвращает ответ.
    ///
    /// Этот метод является ядром взаимодействия с LLM.
    /// Он поддерживает "tool calling" — LLM может запросить выполнение инструментов.
    ///
    /// Пример сценария с tool calling:
    ///   Запрос:  "Покажи содержимое Program.cs"
    ///   Ответ:   tool_calls: [{ function: { name: "read_file", arguments: {"path": "Program.cs"} }}]
    ///   Мы:      выполняем read_file, добавляем результат в историю как role="tool"
    ///   Запрос2: отправляем обновлённую историю
    ///   Ответ2:  "Вот содержимое файла: ..."
    /// </summary>
    /// <param name="messages">
    ///   История разговора. Порядок важен!
    ///   Обычно: [system, user, assistant, tool, assistant, ...]
    /// </param>
    /// <param name="tools">
    ///   Список инструментов которые может вызвать LLM.
    ///   Если null — обычный чат без инструментов.
    /// </param>
    /// <param name="temperature">Температура генерации (0.0–1.0)</param>
    /// <param name="contextWindowSize">Размер контекста в токенах</param>
    public async Task<ChatResponse> ChatAsync(
        List<ChatMessage> messages,
        List<ToolDefinition>? tools = null,
        float temperature = 0.1f,
        int contextWindowSize = 8192)
    {
        // Формируем запрос к Ollama
        var request = new ChatRequest
        {
            Model = _modelName,
            Messages = messages,
            Tools = tools,
            Stream = false, // Ждём полный ответ (не стриминг)
            Options = new GenerationOptions
            {
                Temperature = temperature,
                NumCtx = contextWindowSize
            }
        };

        HttpResponseMessage httpResponse;
        try
        {
            // Отправляем POST запрос к /api/chat
            // PostAsJsonAsync автоматически сериализует объект в JSON
            httpResponse = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/chat",
                request,
                JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            // Если не смогли подключиться к Ollama — даём понятную ошибку
            throw new InvalidOperationException(
                $"Не удалось подключиться к Ollama по адресу '{_baseUrl}'.\n" +
                $"Убедитесь что Docker запущен и выполните: docker compose up -d\n" +
                $"Техническая ошибка: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException)
        {
            // Таймаут — модель слишком долго думает
            throw new InvalidOperationException(
                "Ollama не ответил в течение отведённого времени (5 минут).\n" +
                "Попробуйте более лёгкую модель (например llama3.2:3b) или увеличьте таймаут.");
        }

        // Проверяем HTTP статус ответа
        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync();

            // Специальная обработка типичных ошибок
            if ((int)httpResponse.StatusCode == 404 && errorBody.Contains("model"))
            {
                throw new InvalidOperationException(
                    $"Модель '{_modelName}' не найдена в Ollama.\n" +
                    $"Скачайте её: docker exec -it ollama ollama pull {_modelName}");
            }

            throw new InvalidOperationException(
                $"Ollama вернул ошибку {(int)httpResponse.StatusCode}: {errorBody}");
        }

        // Десериализуем ответ из JSON
        var response = await httpResponse.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);

        if (response == null)
            throw new InvalidOperationException("Ollama вернул пустой ответ");

        // Проверяем на ошибку в теле ответа (Ollama иногда возвращает ошибки с кодом 200)
        if (!string.IsNullOrEmpty(response.Error))
            throw new InvalidOperationException($"Ошибка от Ollama: {response.Error}");

        return response;
    }

    /// <summary>
    /// Стриминговый чат с Ollama — возвращает токены по мере генерации.
    ///
    /// Фаза 3: реализует streaming-вывод (как ChatGPT).
    /// Используется когда LLM даёт финальный текстовый ответ (без tool_calls).
    ///
    /// Пример использования:
    /// <code>
    ///   await foreach (var chunk in ollamaClient.ChatStreamAsync(messages))
    ///       Console.Write(chunk);
    /// </code>
    /// </summary>
    /// <returns>AsyncEnumerable токенов текста</returns>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        List<ChatMessage> messages,
        List<ToolDefinition>? tools = null,
        float temperature = 0.1f,
        int contextWindowSize = 8192,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Model = _modelName,
            Messages = messages,
            Tools = tools,
            Stream = true,
            Options = new GenerationOptions
            {
                Temperature = temperature,
                NumCtx = contextWindowSize
            }
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage httpResponse;
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
            {
                Content = content
            };
            httpResponse = await _httpClient.SendAsync(httpRequest,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Не удалось подключиться к Ollama: {ex.Message}", ex);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Ollama вернул ошибку {(int)httpResponse.StatusCode}: {errorBody}");
        }

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            ChatResponse? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<ChatResponse>(line, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk?.Message?.Content is { Length: > 0 } token)
                yield return token;

            if (chunk?.Done == true)
                break;
        }
    }

    /// <summary>
    /// Проверяет доступность Ollama.
    /// Используется при старте агента чтобы дать понятную ошибку если Ollama не запущен.
    /// </summary>
    /// <returns>true если Ollama доступен и отвечает</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // GET /api/tags возвращает список скачанных моделей
            // Это самый простой способ проверить что Ollama запущен
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/tags",
                HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Любая ошибка = Ollama недоступен
            return false;
        }
    }

    /// <summary>
    /// Возвращает список скачанных моделей в Ollama.
    /// Полезно для диагностики и выбора модели.
    /// </summary>
    public async Task<string[]> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<JsonElement>($"{_baseUrl}/api/tags", JsonOptions);
            if (response.TryGetProperty("models", out var modelsEl))
            {
                return modelsEl.EnumerateArray()
                    .Select(m => m.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            // Логируем в консоль для диагностики, возвращаем пустой список
            Console.Error.WriteLine($"[OllamaClient] Ошибка при получении списка моделей: {ex.Message}");
        }

        return Array.Empty<string>();
    }
}

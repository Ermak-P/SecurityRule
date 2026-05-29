using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAgent.Tools;

namespace DotnetAgent.Mcp;

/// <summary>
/// MCP (Model Context Protocol) сервер — позволяет подключить агента
/// как инструмент к VS Code / Cursor / Claude Desktop.
///
/// Фаза 4: интеграция с внешними AI клиентами через стандартный протокол.
///
/// Протокол MCP:
///   - Транспорт: stdio (stdin/stdout) — JSON-RPC 2.0
///   - Клиент отправляет JSON-RPC запросы в stdin
///   - Сервер отвечает JSON-RPC ответами в stdout
///   - Основные методы: initialize, tools/list, tools/call
///
/// Документация: https://modelcontextprotocol.io/
///
/// Использование (в конфиге VS Code / Cursor):
/// {
///   "mcpServers": {
///     "dotnet-agent": {
///       "command": "dotnet",
///       "args": ["run", "--project", "path/to/DotnetAgent", "--", "--mcp"],
///       "env": { "DOTNET_AGENT_PROJECT_PATH": "C:\\path\\to\\project" }
///     }
///   }
/// }
/// </summary>
public sealed class McpServer
{
    private readonly ToolRegistry _toolRegistry;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public McpServer(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    /// <summary>
    /// Запускает MCP сервер на stdin/stdout.
    /// Работает в бесконечном цикле пока stdin не закроется.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Устанавливаем UTF-8 для stdin/stdout
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line == null) break; // stdin закрылся

            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonRpcRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOpts);
            }
            catch (JsonException)
            {
                await SendErrorAsync(null, -32700, "Parse error");
                continue;
            }

            if (request == null) continue;

            await HandleRequestAsync(request);
        }
    }

    private async Task HandleRequestAsync(JsonRpcRequest request)
    {
        var response = request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCallAsync(request),
            "ping" => new JsonRpcResponse { Id = request.Id, Result = new { } },
            _ => new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" }
            }
        };

        await SendResponseAsync(response);
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { listChanged = false }
                },
                serverInfo = new
                {
                    name = "dotnet-agent",
                    version = "2.0.0"
                }
            }
        };
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = _toolRegistry.GetAllTools().Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = t.Parameters
        }).ToArray();

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new { tools }
        };
    }

    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request)
    {
        string? toolName = null;
        JsonElement? toolArgs = null;

        if (request.Params.HasValue)
        {
            if (request.Params.Value.TryGetProperty("name", out var nameEl))
                toolName = nameEl.GetString();
            if (request.Params.Value.TryGetProperty("arguments", out var argsEl))
                toolArgs = argsEl;
        }

        if (string.IsNullOrEmpty(toolName))
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32602, Message = "Invalid params: name is required" }
            };

        var tool = _toolRegistry.GetTool(toolName);
        if (tool == null)
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32602, Message = $"Tool not found: {toolName}" }
            };

        string result;
        try
        {
            result = await tool.ExecuteAsync(toolArgs ?? JsonDocument.Parse("{}").RootElement);
        }
        catch (Exception ex)
        {
            result = $"Error: {ex.Message}";
        }

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new
            {
                content = new[]
                {
                    new { type = "text", text = result }
                }
            }
        };
    }

    private async Task SendResponseAsync(JsonRpcResponse response)
    {
        var json = JsonSerializer.Serialize(response, JsonOpts);
        await Console.Out.WriteLineAsync(json);
        await Console.Out.FlushAsync();
    }

    private async Task SendErrorAsync(object? id, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        await SendResponseAsync(response);
    }
}

// ─── JSON-RPC 2.0 модели ──────────────────────────────────────────────────────

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
    [JsonPropertyName("id")] public object? Id { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; } = "";
    [JsonPropertyName("params")] public JsonElement? Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
    [JsonPropertyName("id")] public object? Id { get; set; }
    [JsonPropertyName("result")] public object? Result { get; set; }
    [JsonPropertyName("error")] public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

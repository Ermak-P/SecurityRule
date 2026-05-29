using Microsoft.Data.Sqlite;
using DotnetAgent.Models;
using System.Text.Json;

namespace DotnetAgent.Core;

/// <summary>
/// Хранилище сессий разговора в SQLite.
///
/// Фаза 3: сохранение истории разговора между запусками агента.
///
/// База данных хранится в ~/.dotnet-agent/sessions.db
/// Каждая сессия привязана к пути проекта.
///
/// Схема:
///   sessions      — список сессий (id, project_path, created_at, name)
///   messages      — сообщения (session_id, role, content, tool_calls, created_at)
/// </summary>
public sealed class SessionStore : IDisposable
{
    private readonly SqliteConnection _connection;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Создаёт хранилище в указанном файле БД.</summary>
    public SessionStore(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        EnsureSchema();
    }

    /// <summary>Создаёт хранилище в ~/.dotnet-agent/sessions.db</summary>
    public static SessionStore CreateDefault()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dbPath = Path.Combine(home, ".dotnet-agent", "sessions.db");
        return new SessionStore(dbPath);
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                project_path TEXT NOT NULL,
                name        TEXT NOT NULL,
                created_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS messages (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id  INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
                role        TEXT NOT NULL,
                content     TEXT,
                tool_calls  TEXT,
                created_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // ─── Сессии ────────────────────────────────────────────────────────────────

    /// <summary>Создаёт новую сессию и возвращает её id.</summary>
    public long CreateSession(string projectPath, string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sessions (project_path, name)
            VALUES ($path, $name);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$path", projectPath);
        cmd.Parameters.AddWithValue("$name", name);
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    /// <summary>Возвращает последнюю сессию для данного проекта (или null).</summary>
    public SessionInfo? GetLastSession(string projectPath)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, created_at
            FROM sessions
            WHERE project_path = $path
            ORDER BY id DESC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$path", projectPath);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new SessionInfo(reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
    }

    /// <summary>Возвращает список последних N сессий для проекта.</summary>
    public IReadOnlyList<SessionInfo> ListSessions(string projectPath, int limit = 10)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, created_at
            FROM sessions
            WHERE project_path = $path
            ORDER BY id DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$path", projectPath);
        cmd.Parameters.AddWithValue("$limit", limit);
        using var reader = cmd.ExecuteReader();
        var result = new List<SessionInfo>();
        while (reader.Read())
            result.Add(new SessionInfo(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        return result;
    }

    // ─── Сообщения ─────────────────────────────────────────────────────────────

    /// <summary>Сохраняет одно сообщение в сессию.</summary>
    public void SaveMessage(long sessionId, ChatMessage message)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO messages (session_id, role, content, tool_calls)
            VALUES ($sid, $role, $content, $tool_calls);
            """;
        cmd.Parameters.AddWithValue("$sid", sessionId);
        cmd.Parameters.AddWithValue("$role", message.Role);
        cmd.Parameters.AddWithValue("$content", (object?)message.Content ?? DBNull.Value);
        var toolCallsJson = message.ToolCalls?.Count > 0
            ? JsonSerializer.Serialize(message.ToolCalls, JsonOpts)
            : null;
        cmd.Parameters.AddWithValue("$tool_calls", (object?)toolCallsJson ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Загружает историю сообщений для сессии (без system-сообщений).</summary>
    public IReadOnlyList<ChatMessage> LoadMessages(long sessionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT role, content, tool_calls
            FROM messages
            WHERE session_id = $sid
            ORDER BY id ASC;
            """;
        cmd.Parameters.AddWithValue("$sid", sessionId);
        using var reader = cmd.ExecuteReader();
        var result = new List<ChatMessage>();
        while (reader.Read())
        {
            var msg = new ChatMessage
            {
                Role = reader.GetString(0),
                Content = reader.IsDBNull(1) ? null : reader.GetString(1),
            };
            if (!reader.IsDBNull(2))
            {
                var toolCallsJson = reader.GetString(2);
                msg.ToolCalls = JsonSerializer.Deserialize<List<ToolCall>>(toolCallsJson, JsonOpts);
            }
            result.Add(msg);
        }
        return result;
    }

    public void Dispose() => _connection.Dispose();
}

/// <summary>Информация о сессии.</summary>
public record SessionInfo(long Id, string Name, string CreatedAt);

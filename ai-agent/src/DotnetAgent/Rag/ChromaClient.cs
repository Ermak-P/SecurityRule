using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetAgent.Rag;

/// <summary>
/// HTTP клиент для ChromaDB — локальная векторная база данных.
///
/// Фаза 5: RAG (Retrieval-Augmented Generation) — семантический поиск по коду.
///
/// ChromaDB запускается в Docker:
///   docker run -p 8000:8000 chromadb/chroma
///
/// Использование:
///   var chroma = new ChromaClient("http://localhost:8000");
///   await chroma.EnsureCollectionAsync("project-code");
///   await chroma.UpsertAsync("project-code", [("file.cs", "code content", embeddingVector)]);
///   var results = await chroma.QueryAsync("project-code", queryEmbedding, nResults: 5);
///
/// Документация: https://docs.trychroma.com/reference/py-client
/// </summary>
public sealed class ChromaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ChromaClient(HttpClient httpClient, string baseUrl = "http://localhost:8000")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>Проверяет доступность ChromaDB.</summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/heartbeat");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Создаёт коллекцию если она не существует. Возвращает id коллекции.</summary>
    public async Task<string> EnsureCollectionAsync(string name)
    {
        // Сначала пробуем получить существующую
        try
        {
            var existing = await _httpClient.GetFromJsonAsync<ChromaCollection>(
                $"{_baseUrl}/api/v1/collections/{Uri.EscapeDataString(name)}", JsonOpts);
            if (existing?.Id != null) return existing.Id;
        }
        catch (HttpRequestException) { }

        // Создаём новую
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/v1/collections", new
        {
            name,
            metadata = new { description = "DotnetAgent code index" }
        }, JsonOpts);

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ChromaCollection>(JsonOpts);
        return created?.Id ?? throw new InvalidOperationException("ChromaDB не вернул id коллекции");
    }

    /// <summary>
    /// Добавляет или обновляет документы в коллекции.
    /// </summary>
    /// <param name="collectionId">ID коллекции</param>
    /// <param name="documents">Список (id, content, embedding)</param>
    public async Task UpsertAsync(
        string collectionId,
        IReadOnlyList<(string Id, string Content, float[] Embedding)> documents)
    {
        if (documents.Count == 0) return;

        var body = new
        {
            ids = documents.Select(d => d.Id).ToArray(),
            documents = documents.Select(d => d.Content).ToArray(),
            embeddings = documents.Select(d => d.Embedding).ToArray()
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/collections/{collectionId}/upsert", body, JsonOpts);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Выполняет семантический поиск по векторной БД.
    /// </summary>
    /// <param name="collectionId">ID коллекции</param>
    /// <param name="queryEmbedding">Вектор запроса</param>
    /// <param name="nResults">Количество результатов</param>
    /// <returns>Список (id, content, distance) — ближайшие документы</returns>
    public async Task<IReadOnlyList<ChromaQueryResult>> QueryAsync(
        string collectionId,
        float[] queryEmbedding,
        int nResults = 5)
    {
        var body = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = nResults,
            include = new[] { "documents", "distances", "metadatas" }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/collections/{collectionId}/query", body, JsonOpts);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>(JsonOpts);
        if (result == null) return Array.Empty<ChromaQueryResult>();

        var ids = result.Ids?.FirstOrDefault() ?? Array.Empty<string>();
        var documents = result.Documents?.FirstOrDefault() ?? Array.Empty<string>();
        var distances = result.Distances?.FirstOrDefault() ?? Array.Empty<float>();

        return ids.Select((id, i) => new ChromaQueryResult(
            id,
            i < documents.Length ? documents[i] : "",
            i < distances.Length ? distances[i] : 0f
        )).ToList();
    }

    /// <summary>Удаляет документы из коллекции по id.</summary>
    public async Task DeleteAsync(string collectionId, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0) return;
        var body = new { ids };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/collections/{collectionId}/delete", body, JsonOpts);
        response.EnsureSuccessStatusCode();
    }
}

// ─── Модели ChromaDB API ──────────────────────────────────────────────────────

public class ChromaCollection
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class ChromaQueryResponse
{
    [JsonPropertyName("ids")] public string[][]? Ids { get; set; }
    [JsonPropertyName("documents")] public string[][]? Documents { get; set; }
    [JsonPropertyName("distances")] public float[][]? Distances { get; set; }
}

public record ChromaQueryResult(string Id, string Content, float Distance);

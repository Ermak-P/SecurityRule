using System.Text;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetAgent.Rag;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для RAG (Retrieval-Augmented Generation).
///
/// Фаза 5: семантический поиск по коду через ChromaDB.
///
/// Инструменты:
///   - index_project  — индексировать все .cs файлы проекта в ChromaDB
///   - semantic_search — найти код по смыслу (семантический поиск)
///
/// ВАЖНО: ChromaDB должен быть запущен:
///   docker run -d -p 8000:8000 chromadb/chroma
///
/// Для получения embeddings используется Ollama API (/api/embeddings).
/// </summary>
public static class RagTools
{
    public static IEnumerable<IAgentTool> Create(
        string projectPath,
        ChromaClient chromaClient,
        string ollamaUrl,
        string embeddingModel = "nomic-embed-text")
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        return new IAgentTool[]
        {
            new IndexProjectTool(normalizedPath, chromaClient, ollamaUrl, embeddingModel),
            new SemanticSearchTool(normalizedPath, chromaClient, ollamaUrl, embeddingModel),
        };
    }

    // ─── Общее: получение embeddings от Ollama ────────────────────────────────

    private static async Task<float[]> GetEmbeddingAsync(
        HttpClient httpClient,
        string ollamaUrl,
        string model,
        string text)
    {
        // Усекаем текст до ~2000 символов (ограничение контекста embedding моделей)
        if (text.Length > 2000) text = text[..2000];

        var response = await httpClient.PostAsJsonAsync($"{ollamaUrl}/api/embeddings", new
        {
            model,
            prompt = text
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!result.TryGetProperty("embedding", out var embEl))
            throw new InvalidOperationException("Ollama не вернул embedding");

        return embEl.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
    }

    // ─── index_project ────────────────────────────────────────────────────────

    private sealed class IndexProjectTool : IAgentTool
    {
        private readonly string _projectPath;
        private readonly ChromaClient _chroma;
        private readonly string _ollamaUrl;
        private readonly string _embeddingModel;

        public IndexProjectTool(string projectPath, ChromaClient chroma,
            string ollamaUrl, string embeddingModel)
        {
            _projectPath = projectPath;
            _chroma = chroma;
            _ollamaUrl = ollamaUrl;
            _embeddingModel = embeddingModel;
        }

        public string Name => "index_project";

        public string Description =>
            "Индексирует все .cs файлы проекта в ChromaDB для семантического поиска. " +
            "Запусти один раз перед использованием semantic_search. " +
            "Требует ChromaDB (docker run -d -p 8000:8000 chromadb/chroma) и модель " +
            "nomic-embed-text (docker exec -it ollama ollama pull nomic-embed-text).";

        public object Parameters => new
        {
            type = "object",
            properties = new { },
            required = Array.Empty<string>()
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!await _chroma.IsAvailableAsync())
                return "❌ ChromaDB недоступен. Запустите: docker run -d -p 8000:8000 chromadb/chroma";

            string collectionId;
            try
            {
                collectionId = await _chroma.EnsureCollectionAsync("project-code");
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка создания коллекции: {ex.Message}";
            }

            var files = RoslynTools.FindCsFiles(_projectPath).ToList();
            var indexed = 0;
            var errors = new List<string>();

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

            // Индексируем батчами по 10 файлов
            const int batchSize = 10;
            for (var i = 0; i < files.Count; i += batchSize)
            {
                var batch = files.Skip(i).Take(batchSize).ToList();
                var docs = new List<(string Id, string Content, float[] Embedding)>();

                foreach (var file in batch)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var relPath = Path.GetRelativePath(_projectPath, file);
                        var embedding = await GetEmbeddingAsync(httpClient, _ollamaUrl, _embeddingModel, content);
                        docs.Add((relPath, content, embedding));
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file}: {ex.Message}");
                    }
                }

                if (docs.Count > 0)
                {
                    await _chroma.UpsertAsync(collectionId, docs);
                    indexed += docs.Count;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"✅ Проиндексировано {indexed} из {files.Count} файлов.");
            if (errors.Count > 0)
            {
                sb.AppendLine($"⚠️  Ошибки ({errors.Count}):");
                foreach (var e in errors.Take(5)) sb.AppendLine($"  {e}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    // ─── semantic_search ──────────────────────────────────────────────────────

    private sealed class SemanticSearchTool : IAgentTool
    {
        private readonly string _projectPath;
        private readonly ChromaClient _chroma;
        private readonly string _ollamaUrl;
        private readonly string _embeddingModel;

        public SemanticSearchTool(string projectPath, ChromaClient chroma,
            string ollamaUrl, string embeddingModel)
        {
            _projectPath = projectPath;
            _chroma = chroma;
            _ollamaUrl = ollamaUrl;
            _embeddingModel = embeddingModel;
        }

        public string Name => "semantic_search";

        public string Description =>
            "Семантический поиск по коду через ChromaDB. Находит файлы похожие по смыслу на запрос. " +
            "Работает лучше чем текстовый поиск для концептуальных вопросов. " +
            "Требует предварительной индексации через index_project.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Поисковый запрос на естественном языке" },
                n_results = new { type = "integer", description = "Количество результатов (по умолчанию 5)" }
            },
            required = new[] { "query" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("query", out var queryEl))
                return "Ошибка: не передан параметр query";

            var query = queryEl.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
                return "Ошибка: query пустой";

            var nResults = 5;
            if (arguments.TryGetProperty("n_results", out var nEl) && nEl.TryGetInt32(out var n))
                nResults = Math.Clamp(n, 1, 20);

            if (!await _chroma.IsAvailableAsync())
                return "❌ ChromaDB недоступен. Запустите: docker run -d -p 8000:8000 chromadb/chroma";

            string collectionId;
            try
            {
                collectionId = await _chroma.EnsureCollectionAsync("project-code");
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка подключения к ChromaDB: {ex.Message}";
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            float[] queryEmbedding;
            try
            {
                queryEmbedding = await GetEmbeddingAsync(httpClient, _ollamaUrl, _embeddingModel, query);
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка получения embedding: {ex.Message}. " +
                       $"Убедитесь что модель {_embeddingModel} скачана: " +
                       $"docker exec -it ollama ollama pull {_embeddingModel}";
            }

            IReadOnlyList<ChromaQueryResult> results;
            try
            {
                results = await _chroma.QueryAsync(collectionId, queryEmbedding, nResults);
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка поиска: {ex.Message}. Возможно проект ещё не проиндексирован (используй index_project).";
            }

            if (results.Count == 0)
                return "Ничего не найдено. Попробуйте другой запрос или сначала запустите index_project.";

            var sb = new StringBuilder();
            sb.AppendLine($"Найдено {results.Count} результатов для запроса: \"{query}\"\n");

            foreach (var (id, content, distance) in results)
            {
                var similarity = 1f / (1f + distance); // нормализуем L2 distance в [0,1]
                sb.AppendLine($"📄 {id} (схожесть: {similarity:P0})");
                // Показываем первые 20 строк файла как превью
                var preview = string.Join("\n", content.Split('\n').Take(20));
                sb.AppendLine(preview);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}

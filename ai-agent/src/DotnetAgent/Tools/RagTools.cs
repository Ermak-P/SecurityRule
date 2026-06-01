using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetAgent.Rag;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для RAG (Retrieval-Augmented Generation).
///
/// Фаза 5: семантический поиск по коду через ChromaDB.
///
/// Стратегия чанкинга:
///   - Roslyn разбивает .cs файлы на отдельные методы и классы
///   - Каждый чанк = один метод или standalone-класс
///   - Хранятся в отдельных коллекциях ChromaDB по слоям: code-domain, code-infrastructure, code-web, code-tests
///
/// Инкрементальная переиндексация:
///   - SHA256 хеш файла сохраняется в метаданных
///   - При переиндексации файлы с неизменившимся хешем пропускаются
///
/// Инструменты:
///   - index_project    — индексировать .cs файлы проекта в ChromaDB (Roslyn-чанки)
///   - semantic_search  — найти код по смыслу (семантический поиск), опционально по слою
///
/// ВАЖНО: ChromaDB должен быть запущен:
///   docker compose up -d (в папке ai-agent)
/// </summary>
public static class RagTools
{
    // Имена коллекций по слоям архитектуры
    private const string LayerDomain = "domain";
    private const string LayerInfrastructure = "infrastructure";
    private const string LayerWeb = "web";
    private const string LayerTests = "tests";
    private const string LayerDefault = "default";

    private static readonly string[] AllLayers =
        [LayerDomain, LayerInfrastructure, LayerWeb, LayerTests, LayerDefault];

    private static string CollectionName(string layer) => $"code-{layer}";

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

    private const int MaxEmbeddingTextLength = 2000;

    // ─── Получение embeddings от Ollama ──────────────────────────────────────

    private static async Task<float[]> GetEmbeddingAsync(
        HttpClient httpClient,
        string ollamaUrl,
        string model,
        string text)
    {
        // Усекаем текст до ~2000 символов (ограничение контекста embedding моделей)
        if (text.Length > MaxEmbeddingTextLength) text = text[..MaxEmbeddingTextLength];

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

    // ─── Определение слоя по пути файла ──────────────────────────────────────

    private static string DetectLayer(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        if (normalized.Contains("/Domain/") || normalized.Contains(".Domain/"))
            return LayerDomain;
        if (normalized.Contains("/Infrastructure/") || normalized.Contains(".Infrastructure/"))
            return LayerInfrastructure;
        if (normalized.Contains(".E2E.") || normalized.Contains(".Tests/") || normalized.Contains("/Tests/"))
            return LayerTests;
        if (normalized.Contains("/Web/") || normalized.Contains(".Web/") || normalized.EndsWith(".razor"))
            return LayerWeb;
        return LayerDefault;
    }

    // ─── Roslyn-чанкинг: методы и классы ─────────────────────────────────────

    private sealed record CodeChunk(
        string Id,           // "{relPath}::{MemberName}"
        string Content,      // текст метода/класса
        string FilePath,     // относительный путь к файлу
        string MemberName,   // имя метода или класса
        string MemberType,   // "method", "class", "interface", "record", "struct"
        string? EntityName,  // имя класса-контейнера (для методов)
        string Layer,        // domain / infrastructure / web / tests / default
        string FileHash      // SHA256 файла (для инкрементальной переиндексации)
    );

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..16]; // 16 символов достаточно
    }

    /// <summary>Разбивает один .cs файл на чанки методов и классов через Roslyn.</summary>
    private static IReadOnlyList<CodeChunk> ChunkFile(string filePath, string relPath, string layer)
    {
        string code;
        try { code = File.ReadAllText(filePath); }
        catch { return []; }

        var fileHash = ComputeHash(code);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var chunks = new List<CodeChunk>();

        // Обходим все объявления типов верхнего уровня
        var typeDecls = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(t => t.Parent is not TypeDeclarationSyntax) // только верхний уровень
            .ToList();

        foreach (var typeDecl in typeDecls)
        {
            var typeName = typeDecl.Identifier.Text;
            var memberType = typeDecl switch
            {
                InterfaceDeclarationSyntax => "interface",
                RecordDeclarationSyntax => "record",
                StructDeclarationSyntax => "struct",
                _ => "class"
            };

            // Методы внутри класса
            var methods = typeDecl.Members.OfType<MethodDeclarationSyntax>().ToList();

            if (methods.Count == 0)
            {
                // Класс без методов — индексируем как единый чанк
                var classText = typeDecl.ToFullString();
                if (classText.Length > 50)
                {
                    chunks.Add(new CodeChunk(
                        Id: $"{relPath}::{typeName}",
                        Content: classText.Length > 4000 ? classText[..4000] : classText,
                        FilePath: relPath,
                        MemberName: typeName,
                        MemberType: memberType,
                        EntityName: null,
                        Layer: layer,
                        FileHash: fileHash
                    ));
                }
            }
            else
            {
                // Индексируем каждый метод отдельно
                foreach (var method in methods)
                {
                    var methodName = method.Identifier.Text;
                    var methodText = method.ToFullString();

                    if (methodText.Length < 20) continue; // пропускаем тривиальные

                    chunks.Add(new CodeChunk(
                        Id: $"{relPath}::{typeName}.{methodName}",
                        Content: methodText.Length > 4000 ? methodText[..4000] : methodText,
                        FilePath: relPath,
                        MemberName: methodName,
                        MemberType: "method",
                        EntityName: typeName,
                        Layer: layer,
                        FileHash: fileHash
                    ));
                }
            }
        }

        // Если Roslyn не нашёл структур — индексируем весь файл как один чанк
        if (chunks.Count == 0 && code.Length > 50)
        {
            chunks.Add(new CodeChunk(
                Id: relPath,
                Content: code.Length > 4000 ? code[..4000] : code,
                FilePath: relPath,
                MemberName: Path.GetFileNameWithoutExtension(relPath),
                MemberType: "file",
                EntityName: null,
                Layer: layer,
                FileHash: fileHash
            ));
        }

        return chunks;
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
            "Индексирует .cs файлы проекта в ChromaDB для семантического поиска. " +
            "Использует Roslyn для разбивки на чанки (метод/класс). " +
            "Коллекции разделены по слоям: code-domain, code-infrastructure, code-web, code-tests. " +
            "Поддерживает инкрементальную переиндексацию — пропускает неизменённые файлы. " +
            "Запусти один раз перед использованием semantic_search. " +
            "Требует ChromaDB (docker compose up -d) и модель nomic-embed-text в Ollama.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                force = new
                {
                    type = "boolean",
                    description = "true — переиндексировать все файлы, даже неизменённые. " +
                                  "По умолчанию false — инкрементальная индексация."
                }
            },
            required = Array.Empty<string>()
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            var force = false;
            if (arguments.TryGetProperty("force", out var forceEl) && forceEl.ValueKind == JsonValueKind.True)
                force = true;

            if (!await _chroma.IsAvailableAsync())
                return "❌ ChromaDB недоступен. Запустите: docker compose up -d (в папке ai-agent)";

            // Создаём коллекции для всех слоёв
            var collectionIds = new Dictionary<string, string>();
            foreach (var layer in AllLayers)
            {
                try
                {
                    collectionIds[layer] = await _chroma.EnsureCollectionAsync(CollectionName(layer));
                }
                catch (Exception ex)
                {
                    return $"❌ Ошибка создания коллекции {CollectionName(layer)}: {ex.Message}";
                }
            }

            // Собираем все .cs файлы и разбиваем на чанки
            var files = RoslynTools.FindCsFiles(_projectPath).ToList();
            var allChunks = new List<CodeChunk>();

            foreach (var file in files)
            {
                var relPath = Path.GetRelativePath(_projectPath, file);
                var layer = DetectLayer(file);
                var fileChunks = ChunkFile(file, relPath, layer);
                allChunks.AddRange(fileChunks);
            }

            // Если не force — загружаем существующие хеши из ChromaDB для сравнения
            // Группируем чанки по слоям и загружаем батчами
            var chunksByLayer = allChunks.GroupBy(c => c.Layer).ToList();
            var totalIndexed = 0;
            var totalSkipped = 0;
            var errors = new List<string>();

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };

            const int batchSize = 5; // небольшие батчи для стабильности

            foreach (var layerGroup in chunksByLayer)
            {
                var layer = layerGroup.Key;
                var collectionId = collectionIds[layer];
                var layerChunks = layerGroup.ToList();

                // Загружаем хеши существующих чанков через ChromaDB GET endpoint
                var existingChunkHashes = force
                    ? new Dictionary<string, string>()
                    : await _chroma.GetItemHashesAsync(collectionId);

                for (var i = 0; i < layerChunks.Count; i += batchSize)
                {
                    var batch = layerChunks.Skip(i).Take(batchSize).ToList();
                    var toUpsert = new List<(string, string, float[], Dictionary<string, string>?)>();

                    foreach (var chunk in batch)
                    {
                        // Инкрементальная переиндексация: пропускаем если хеш не изменился
                        if (!force && existingChunkHashes.TryGetValue(chunk.Id, out var storedHash)
                            && storedHash == chunk.FileHash)
                        {
                            totalSkipped++;
                            continue;
                        }

                        try
                        {
                            var embedding = await GetEmbeddingAsync(
                                httpClient, _ollamaUrl, _embeddingModel, chunk.Content);

                            var metadata = new Dictionary<string, string>
                            {
                                ["layer"] = chunk.Layer,
                                ["file"] = chunk.FilePath,
                                ["member"] = chunk.MemberName,
                                ["member_type"] = chunk.MemberType,
                                ["file_hash"] = chunk.FileHash
                            };
                            if (chunk.EntityName != null)
                                metadata["entity"] = chunk.EntityName;

                            toUpsert.Add((chunk.Id, chunk.Content, embedding, metadata));
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{chunk.Id}: {ex.Message}");
                        }
                    }

                    if (toUpsert.Count > 0)
                    {
                        await _chroma.UpsertAsync(collectionId, toUpsert);
                        totalIndexed += toUpsert.Count;
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"✅ Индексация завершена:");
            sb.AppendLine($"   Файлов обработано: {files.Count}");
            sb.AppendLine($"   Чанков создано: {allChunks.Count}");
            sb.AppendLine($"   Проиндексировано: {totalIndexed}");
            if (totalSkipped > 0) sb.AppendLine($"   Пропущено (без изменений): {totalSkipped}");

            foreach (var layer in AllLayers)
            {
                var count = allChunks.Count(c => c.Layer == layer);
                if (count > 0) sb.AppendLine($"   [{layer}]: {count} чанков");
            }

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
            "Семантический поиск по коду через ChromaDB. Находит методы и классы похожие по смыслу на запрос. " +
            "Работает лучше чем текстовый поиск для концептуальных вопросов. " +
            "Поддерживает фильтрацию по слою архитектуры (domain, infrastructure, web, tests). " +
            "Требует предварительной индексации через index_project.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Поисковый запрос на естественном языке" },
                layer = new
                {
                    type = "string",
                    description = "Фильтр по слою: domain, infrastructure, web, tests. Если не указан — поиск по всем слоям.",
                    @enum = new[] { "domain", "infrastructure", "web", "tests" }
                },
                n_results = new { type = "integer", description = "Количество результатов (по умолчанию 5, максимум 20)" }
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

            string? layerFilter = null;
            if (arguments.TryGetProperty("layer", out var layerEl))
                layerFilter = layerEl.GetString()?.ToLowerInvariant()?.Trim();

            if (!await _chroma.IsAvailableAsync())
                return "❌ ChromaDB недоступен. Запустите: docker compose up -d (в папке ai-agent)";

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
            float[] queryEmbedding;
            try
            {
                queryEmbedding = await GetEmbeddingAsync(httpClient, _ollamaUrl, _embeddingModel, query);
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка получения embedding: {ex.Message}. " +
                       $"Убедитесь что модель {_embeddingModel} скачана: " +
                       $"ollama pull {_embeddingModel}";
            }

            // Определяем какие коллекции искать
            var layersToSearch = string.IsNullOrEmpty(layerFilter)
                ? AllLayers
                : new[] { layerFilter };

            var allResults = new List<(ChromaQueryResult Result, string Layer)>();

            foreach (var layer in layersToSearch)
            {
                string collectionId;
                try
                {
                    collectionId = await _chroma.EnsureCollectionAsync(CollectionName(layer));
                }
                catch
                {
                    continue;
                }

                try
                {
                    var results = await _chroma.QueryAsync(
                        collectionId, queryEmbedding, nResults);
                    allResults.AddRange(results.Select(r => (r, layer)));
                }
                catch
                {
                    // Коллекция могла быть пуста — пропускаем
                }
            }

            if (allResults.Count == 0)
                return "Ничего не найдено. Попробуйте другой запрос или сначала запустите index_project.";

            // Сортируем все результаты по дистанции и берём top-N
            var topResults = allResults
                .OrderBy(x => x.Result.Distance)
                .Take(nResults)
                .ToList();

            var sb = new StringBuilder();
            var layerDesc = string.IsNullOrEmpty(layerFilter) ? "всем слоям" : layerFilter;
            sb.AppendLine($"Найдено {topResults.Count} результатов по запросу: \"{query}\" (слой: {layerDesc})\n");

            foreach (var (result, layer) in topResults)
            {
                var similarity = 1f / (1f + result.Distance);
                var memberType = result.Metadata?.GetValueOrDefault("member_type") ?? "?";
                var member = result.Metadata?.GetValueOrDefault("member") ?? result.Id;
                var entity = result.Metadata?.GetValueOrDefault("entity");
                var file = result.Metadata?.GetValueOrDefault("file") ?? result.Id;
                var displayName = entity != null ? $"{entity}.{member}" : member;

                sb.AppendLine($"📄 [{layer}] {displayName} ({memberType}) — {file} (схожесть: {similarity:P0})");
                // Показываем первые 15 строк чанка как превью
                var preview = string.Join("\n", result.Content.Split('\n').Take(15));
                sb.AppendLine(preview);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}

using System.Text;
using System.Text.Json;

namespace DotnetAgent.Tools;

/// <summary>
/// Набор инструментов для работы с файловой системой .NET проекта.
///
/// Содержит 5 инструментов:
///   - list_files      — просмотр структуры проекта
///   - read_file       — чтение содержимого файла
///   - write_file      — изменение существующего файла
///   - create_file     — создание нового файла
///   - search_in_files — поиск текста во всех файлах
///
/// БЕЗОПАСНОСТЬ:
///   Все операции ограничены директорией projectPath.
///   Попытка обратиться к файлу за пределами проекта вызывает исключение.
///   Это предотвращает случайное (или намеренное через LLM) изменение
///   системных файлов или файлов других проектов.
/// </summary>
public static class FileSystemTools
{
    /// <summary>
    /// Создаёт и возвращает все инструменты для работы с файловой системой.
    /// </summary>
    /// <param name="projectPath">Абсолютный путь до корня .NET проекта</param>
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        // Нормализуем путь один раз для всех инструментов
        var normalizedPath = Path.GetFullPath(projectPath);

        return new IAgentTool[]
        {
            new ListFilesTool(normalizedPath),
            new ReadFileTool(normalizedPath),
            new WriteFileTool(normalizedPath),
            new CreateFileTool(normalizedPath),
            new SearchInFilesTool(normalizedPath)
        };
    }

    // ─── Общие вспомогательные методы ─────────────────────────────────────────

    /// <summary>
    /// Разрешает и проверяет путь к файлу.
    ///
    /// Принимает относительный путь от пользователя/LLM,
    /// превращает его в абсолютный и проверяет что он находится
    /// внутри директории проекта.
    ///
    /// Защита от path traversal:
    ///   Атака: relativePath = "../../Windows/System32/important.dll"
    ///   После Path.GetFullPath() получим C:\Windows\System32\important.dll
    ///   Проверка !fullPath.StartsWith(projectPath) обнаружит выход за пределы
    /// </summary>
    private static string ResolveSafePath(string projectPath, string relativePath)
    {
        // Убираем кавычки если LLM добавил их
        relativePath = relativePath.Trim('"', '\'');

        // Строим абсолютный путь
        var fullPath = Path.IsPathRooted(relativePath)
            ? Path.GetFullPath(relativePath)
            : Path.GetFullPath(Path.Combine(projectPath, relativePath));

        // Проверяем что путь внутри проекта
        // Добавляем DirectorySeparatorChar чтобы избежать false-positive:
        //   projectPath = "C:\Projects\App"
        //   fullPath    = "C:\Projects\AppOther\file.cs" — должно быть запрещено!
        //   Без добавления сепаратора StartsWith вернул бы true
        var projectRoot = projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Доступ запрещён: путь '{relativePath}' выходит за пределы директории проекта '{projectPath}'");
        }

        return fullPath;
    }

    /// <summary>
    /// Папки которые нужно пропускать при обходе файловой структуры.
    /// Содержат артефакты сборки и системные файлы, не интересные для анализа.
    /// </summary>
    private static readonly HashSet<string> SkipDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj",       // Артефакты сборки .NET
        ".git", ".svn",     // Системные папки систем контроля версий
        ".vs", ".idea",     // Папки IDE (Visual Studio, Rider)
        "node_modules",     // JavaScript зависимости
        ".terraform",       // Terraform (если есть инфраструктурный код)
    };

    // ─── Инструмент: list_files ───────────────────────────────────────────────

    /// <summary>
    /// Инструмент для просмотра файловой структуры проекта.
    ///
    /// LLM обычно вызывает этот инструмент первым чтобы понять:
    ///   - Как организован проект (MVC? Clean Architecture? Minimal API?)
    ///   - Где найти нужные файлы (контроллеры, сервисы, модели)
    ///   - Какие технологии используются (наличие .razor, .proto и т.д.)
    /// </summary>
    private class ListFilesTool(string projectPath) : IAgentTool
    {
        /// <summary>
        /// Максимальный размер вывода list_files в символах.
        ///
        /// Ограничение нужно чтобы не переполнять контекстное окно LLM:
        ///   8192 токенов ≈ ~32 000 символов.
        /// Вывод без фильтра на большом проекте легко достигает 100 000+ символов
        /// и вытесняет из контекста задачу пользователя.
        /// При превышении лимита агент получает подсказку использовать фильтры.
        /// </summary>
        private const int MaxOutputChars = 4000;

        public string Name => "list_files";

        public string Description =>
            "Показывает структуру файлов и папок в .NET проекте в виде дерева. " +
            "Используй этот инструмент в начале работы чтобы понять организацию проекта. " +
            "Папки bin/, obj/, .git/ автоматически скрыты. " +
            "Опционально можно указать поддиректорию и фильтр расширений.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                subdirectory = new
                {
                    type = "string",
                    description = "Поддиректория для детального просмотра (необязательно). " +
                                  "Пример: 'src' или 'Controllers' или 'src/Services'"
                },
                extension_filter = new
                {
                    type = "string",
                    description = "Показывать только файлы с этим расширением (необязательно). " +
                                  "Пример: '.cs' — только C# файлы, '.razor' — только Razor компоненты"
                }
            },
            required = Array.Empty<string>() // Все параметры необязательны
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            // Читаем необязательные аргументы
            var subdirectory = arguments.TryGetProperty("subdirectory", out var subEl)
                ? subEl.GetString() ?? "" : "";
            var extensionFilter = arguments.TryGetProperty("extension_filter", out var extEl)
                ? extEl.GetString() ?? "" : "";

            // Определяем точку начала обхода
            string targetPath;
            try
            {
                targetPath = string.IsNullOrEmpty(subdirectory)
                    ? projectPath
                    : ResolveSafePath(projectPath, subdirectory);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult($"⛔ {ex.Message}");
            }

            if (!Directory.Exists(targetPath))
                return Task.FromResult($"❌ Директория не найдена: {subdirectory}");

            // Строим дерево файлов
            var sb = new StringBuilder();
            var displayRoot = string.IsNullOrEmpty(subdirectory) ? "." : subdirectory;
            sb.AppendLine($"📂 {displayRoot}/");

            AppendDirectoryTree(targetPath, projectPath, sb, "  ", extensionFilter, depth: 0, maxDepth: 5);

            var result = sb.ToString();

            // Защита от переполнения контекста: обрезаем слишком длинный вывод
            if (result.Length > MaxOutputChars)
            {
                result = result[..MaxOutputChars] +
                         $"\n...\n⚠️ Вывод обрезан (лимит {MaxOutputChars} символов) — слишком много файлов." +
                         " Используй параметры subdirectory или extension_filter чтобы сузить область поиска." +
                         " Например: subdirectory='src/SecurityRule.Web/Components', extension_filter='.razor'";
            }

            return Task.FromResult(result);
        }

        private static void AppendDirectoryTree(
            string currentDir, string projectPath, StringBuilder sb,
            string indent, string extensionFilter, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            // Пропускаем системные папки
            var dirName = Path.GetFileName(currentDir);
            if (SkipDirectories.Contains(dirName) && depth > 0) return;

            // Получаем и сортируем содержимое
            string[] files;
            string[] subdirs;
            try
            {
                files = Directory.GetFiles(currentDir).OrderBy(f => f).ToArray();
                subdirs = Directory.GetDirectories(currentDir).OrderBy(d => d).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                return; // Нет доступа — пропускаем
            }

            // Выводим файлы
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();

                // Применяем фильтр расширений
                if (!string.IsNullOrEmpty(extensionFilter) &&
                    !ext.Equals(extensionFilter.TrimStart('.') is { } cleaned
                        ? $".{cleaned}"
                        : extensionFilter,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                // Иконка по типу файла
                var icon = ext switch
                {
                    ".cs" => "📄",
                    ".csproj" or ".vbproj" or ".fsproj" => "🔧",
                    ".sln" or ".slnx" => "📦",
                    ".json" => "📋",
                    ".xml" => "📋",
                    ".razor" or ".cshtml" or ".html" => "🌐",
                    ".md" => "📝",
                    ".yml" or ".yaml" => "⚙️",
                    ".sh" or ".ps1" or ".cmd" => "💻",
                    ".sql" => "🗃️",
                    ".css" or ".scss" => "🎨",
                    ".js" or ".ts" => "🟨",
                    _ => "📄"
                };

                sb.AppendLine($"{indent}{icon} {Path.GetFileName(file)}");
            }

            // Рекурсивно выводим поддиректории
            foreach (var subdir in subdirs)
            {
                var subName = Path.GetFileName(subdir);
                if (SkipDirectories.Contains(subName)) continue;

                sb.AppendLine($"{indent}📁 {subName}/");
                AppendDirectoryTree(subdir, projectPath, sb, indent + "  ", extensionFilter, depth + 1, maxDepth);
            }
        }
    }

    // ─── Инструмент: read_file ────────────────────────────────────────────────

    /// <summary>
    /// Инструмент для чтения содержимого файла.
    ///
    /// Агент ВСЕГДА должен прочитать файл перед его изменением!
    /// Это обеспечивает точное редактирование без потери существующего кода.
    ///
    /// Ограничение размера: файлы больше 100 KB не читаются целиком.
    /// Для больших файлов используйте search_in_files.
    /// </summary>
    private class ReadFileTool(string projectPath) : IAgentTool
    {
        // Максимальный размер файла для чтения (100 KB)
        // Большие файлы займут много места в контексте LLM
        private const int MaxFileSizeBytes = 100_000;

        public string Name => "read_file";

        public string Description =>
            "Читает и возвращает полное содержимое файла. " +
            "ВАЖНО: всегда вызывай этот инструмент перед write_file! " +
            "Ограничение: файлы больше 100 KB не читаются (используй search_in_files для поиска фрагментов).";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                path = new
                {
                    type = "string",
                    description = "Путь к файлу относительно корня проекта. " +
                                  "Примеры: 'Program.cs', 'src/Services/UserService.cs', 'appsettings.json'"
                }
            },
            required = new[] { "path" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("path", out var pathEl) || pathEl.ValueKind == JsonValueKind.Null)
                return Task.FromResult("❌ Ошибка: параметр 'path' обязателен");

            var relativePath = pathEl.GetString() ?? "";

            string fullPath;
            try
            {
                fullPath = ResolveSafePath(projectPath, relativePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult($"⛔ {ex.Message}");
            }

            if (!File.Exists(fullPath))
                return Task.FromResult($"❌ Файл не найден: '{relativePath}'");

            // Проверяем размер файла
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxFileSizeBytes)
                return Task.FromResult(
                    $"⚠️ Файл '{relativePath}' слишком большой ({fileInfo.Length / 1024} KB, лимит {MaxFileSizeBytes / 1024} KB).\n" +
                    $"Используй search_in_files для поиска нужного фрагмента.");

            try
            {
                var content = File.ReadAllText(fullPath, Encoding.UTF8);
                var displayPath = Path.GetRelativePath(projectPath, fullPath).Replace('\\', '/');

                // Добавляем заголовок чтобы LLM знал из какого файла контент
                return Task.FromResult(
                    $"// Файл: {displayPath} ({fileInfo.Length} байт)\n\n{content}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"❌ Ошибка чтения файла '{relativePath}': {ex.Message}");
            }
        }
    }

    // ─── Инструмент: write_file ───────────────────────────────────────────────

    /// <summary>
    /// Инструмент для перезаписи содержимого файла.
    ///
    /// ВАЖНО: этот инструмент ПОЛНОСТЬЮ заменяет файл!
    /// Перед вызовом LLM должен:
    ///   1. Прочитать файл через read_file
    ///   2. Сформировать новое полное содержимое
    ///   3. Записать через write_file
    ///
    /// Создаётся резервная копия (.bak) на случай ошибки.
    /// </summary>
    private class WriteFileTool(string projectPath) : IAgentTool
    {
        public string Name => "write_file";

        public string Description =>
            "Полностью заменяет содержимое существующего файла. " +
            "ОБЯЗАТЕЛЬНО вызови read_file перед этим инструментом! " +
            "Передай ПОЛНОЕ новое содержимое файла (не только изменённые части). " +
            "Для создания НОВОГО файла используй create_file.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                path = new
                {
                    type = "string",
                    description = "Путь к файлу относительно корня проекта"
                },
                content = new
                {
                    type = "string",
                    description = "Новое ПОЛНОЕ содержимое файла (весь файл целиком)"
                }
            },
            required = new[] { "path", "content" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("path", out var pathEl))
                return Task.FromResult("❌ Ошибка: параметр 'path' обязателен");
            if (!arguments.TryGetProperty("content", out var contentEl))
                return Task.FromResult("❌ Ошибка: параметр 'content' обязателен");

            var relativePath = pathEl.GetString() ?? "";
            var content = contentEl.GetString() ?? "";

            string fullPath;
            try
            {
                fullPath = ResolveSafePath(projectPath, relativePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult($"⛔ {ex.Message}");
            }

            if (!File.Exists(fullPath))
                return Task.FromResult(
                    $"❌ Файл не существует: '{relativePath}'. " +
                    $"Для создания нового файла используй инструмент create_file.");

            // Создаём резервную копию перед изменением
            var backupPath = fullPath + ".bak";
            try
            {
                File.Copy(fullPath, backupPath, overwrite: true);
                File.WriteAllText(fullPath, content, Encoding.UTF8);

                // Удаляем бэкап если всё прошло успешно
                File.Delete(backupPath);

                var displayPath = Path.GetRelativePath(projectPath, fullPath).Replace('\\', '/');
                return Task.FromResult($"✅ Файл обновлён: {displayPath} ({content.Length} символов)");
            }
            catch (Exception ex)
            {
                // При ошибке восстанавливаем из бэкапа
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, fullPath, overwrite: true);
                        File.Delete(backupPath);
                    }
                    catch (Exception restoreEx)
                    {
                        return Task.FromResult(
                            $"❌ Критическая ошибка: не удалось записать файл ({ex.Message}) " +
                            $"и не удалось восстановить из резервной копии ({restoreEx.Message}). " +
                            $"Бэкап находится по адресу: {backupPath}");
                    }
                }
                return Task.FromResult($"❌ Ошибка записи файла '{relativePath}': {ex.Message}");
            }
        }
    }

    // ─── Инструмент: create_file ──────────────────────────────────────────────

    /// <summary>
    /// Инструмент для создания нового файла.
    ///
    /// Автоматически создаёт все необходимые директории.
    /// Не перезаписывает существующие файлы.
    /// </summary>
    private class CreateFileTool(string projectPath) : IAgentTool
    {
        public string Name => "create_file";

        public string Description =>
            "Создаёт новый файл с указанным содержимым. " +
            "Директории создаются автоматически. " +
            "Возвращает ошибку если файл уже существует — используй write_file для изменения существующих файлов.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                path = new
                {
                    type = "string",
                    description = "Путь к новому файлу относительно корня проекта. " +
                                  "Пример: 'src/Services/EmailService.cs'"
                },
                content = new
                {
                    type = "string",
                    description = "Содержимое нового файла"
                }
            },
            required = new[] { "path", "content" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("path", out var pathEl))
                return Task.FromResult("❌ Ошибка: параметр 'path' обязателен");
            if (!arguments.TryGetProperty("content", out var contentEl))
                return Task.FromResult("❌ Ошибка: параметр 'content' обязателен");

            var relativePath = pathEl.GetString() ?? "";
            var content = contentEl.GetString() ?? "";

            string fullPath;
            try
            {
                fullPath = ResolveSafePath(projectPath, relativePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult($"⛔ {ex.Message}");
            }

            if (File.Exists(fullPath))
                return Task.FromResult(
                    $"❌ Файл уже существует: '{relativePath}'. " +
                    $"Используй write_file для изменения существующего файла.");

            try
            {
                // Создаём директории если не существуют
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(fullPath, content, Encoding.UTF8);

                var displayPath = Path.GetRelativePath(projectPath, fullPath).Replace('\\', '/');
                return Task.FromResult($"✅ Файл создан: {displayPath} ({content.Length} символов)");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"❌ Ошибка создания файла '{relativePath}': {ex.Message}");
            }
        }
    }

    // ─── Инструмент: search_in_files ──────────────────────────────────────────

    /// <summary>
    /// Инструмент для поиска текста во всех файлах проекта.
    ///
    /// Полезен для:
    ///   - Поиска использований класса или метода
    ///   - Нахождения TODO комментариев
    ///   - Поиска конфигурационных значений
    ///   - Анализа зависимостей между файлами
    ///
    /// Показывает контекст (строки вокруг совпадения) для лучшего понимания.
    /// </summary>
    private class SearchInFilesTool(string projectPath) : IAgentTool
    {
        // Максимальное количество результатов (защита от вывала огромного текста в LLM)
        private const int MaxResults = 30;

        public string Name => "search_in_files";

        public string Description =>
            "Ищет текст во всех файлах проекта и возвращает строки с совпадениями (с контекстом). " +
            "Используй для: поиска класса/метода, поиска использований, поиска TODO, поиска конфигурации. " +
            $"Показывает максимум {MaxResults} результатов.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                query = new
                {
                    type = "string",
                    description = "Текст для поиска. Пример: 'UserService', 'TODO', 'ConnectionString'"
                },
                extensions = new
                {
                    type = "string",
                    description = "Расширения файлов через запятую (необязательно, по умолчанию .cs). " +
                                  "Пример: '.cs,.razor' или '.json,.xml'"
                },
                case_sensitive = new
                {
                    type = "boolean",
                    description = "Учитывать регистр при поиске (по умолчанию false — не учитывать)"
                }
            },
            required = new[] { "query" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("query", out var queryEl) || queryEl.ValueKind == JsonValueKind.Null)
                return Task.FromResult("❌ Ошибка: параметр 'query' обязателен");

            var query = queryEl.GetString() ?? "";
            if (string.IsNullOrEmpty(query))
                return Task.FromResult("❌ Ошибка: поисковый запрос не может быть пустым");

            // Разбираем расширения файлов
            var extensionsStr = arguments.TryGetProperty("extensions", out var extEl) && extEl.ValueKind == JsonValueKind.String
                ? extEl.GetString() ?? ".cs"
                : ".cs";
            var extensions = extensionsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .Select(e => e.StartsWith('.') ? e : $".{e}")
                .ToHashSet();

            var caseSensitive = arguments.TryGetProperty("case_sensitive", out var csEl)
                && csEl.ValueKind == JsonValueKind.True;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            var results = new StringBuilder();
            var matchCount = 0;
            var filesSearched = 0;

            // Обходим все файлы проекта рекурсивно
            foreach (var file in Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories))
            {
                // Пропускаем папки bin/obj (там артефакты сборки)
                if (IsInSkippedDirectory(file)) continue;

                // Фильтр по расширению
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!extensions.Contains(ext)) continue;

                filesSearched++;
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(file, Encoding.UTF8);
                }
                catch
                {
                    continue; // Пропускаем файлы которые не можем прочитать
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains(query, comparison)) continue;

                    var displayPath = Path.GetRelativePath(projectPath, file).Replace('\\', '/');

                    // Первое совпадение — добавляем заголовок
                    if (matchCount == 0)
                        results.AppendLine($"Результаты поиска '{query}' ({extensionsStr}):\n");

                    results.AppendLine($"📄 {displayPath}:{i + 1}");

                    // Показываем контекст: 2 строки до и после совпадения
                    var contextStart = Math.Max(0, i - 2);
                    var contextEnd = Math.Min(lines.Length - 1, i + 2);
                    for (int j = contextStart; j <= contextEnd; j++)
                    {
                        // Выделяем строку с совпадением маркером >>>
                        var marker = j == i ? ">>>" : "   ";
                        results.AppendLine($"  {marker} {j + 1,4}: {lines[j]}");
                    }
                    results.AppendLine();

                    matchCount++;

                    // Лимит результатов
                    if (matchCount >= MaxResults)
                    {
                        results.AppendLine($"... (показаны первые {MaxResults} совпадений, поиск прерван)");
                        break;
                    }
                }

                if (matchCount >= MaxResults) break;
            }

            if (matchCount == 0)
                return Task.FromResult(
                    $"Текст '{query}' не найден в файлах с расширением {extensionsStr}. " +
                    $"Проверено файлов: {filesSearched}.");

            results.AppendLine($"Итого: {matchCount} совпадений в {filesSearched} проверенных файлах.");
            return Task.FromResult(results.ToString());
        }

        private static bool IsInSkippedDirectory(string filePath)
        {
            // Проверяем каждый компонент пути
            var parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return parts.Any(part => SkipDirectories.Contains(part));
        }
    }
}

using System.Text;
using System.Text.Json;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для работы с папкой .github.
///
/// Папка .github может находиться в родительской директории относительно пути проекта.
/// Агент ищет её поднимаясь вверх по дереву директорий.
///
/// Доступные инструменты:
///   - read_github_file — читает файл из папки .github (например, workflow, шаблоны PR)
///
/// Статические методы:
///   - FindGithubRoot    — ищет папку .github вверх по дереву
///   - LoadCopilotInstructions — возвращает содержимое .github/copilot-instructions.md
/// </summary>
public static class GithubTools
{
    /// <summary>
    /// Создаёт инструменты .github. Возвращает пустой список если папка не найдена.
    /// </summary>
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        var githubRoot = FindGithubRoot(projectPath);
        if (githubRoot == null)
            return Array.Empty<IAgentTool>();

        return new IAgentTool[]
        {
            new ReadGithubFileTool(githubRoot),
            new ListGithubFilesTool(githubRoot),
        };
    }

    /// <summary>
    /// Ищет папку .github поднимаясь вверх по дереву директорий.
    /// Возвращает путь к папке .github или null если не найдено.
    /// </summary>
    public static string? FindGithubRoot(string startPath)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, ".github");
            if (Directory.Exists(candidate))
                return candidate;
            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }
        return null;
    }

    /// <summary>
    /// Загружает содержимое .github/copilot-instructions.md.
    /// Возвращает null если файл не найден.
    /// </summary>
    public static string? LoadCopilotInstructions(string projectPath)
    {
        var githubRoot = FindGithubRoot(projectPath);
        if (githubRoot == null)
            return null;

        var instructionsPath = Path.Combine(githubRoot, "copilot-instructions.md");
        if (!File.Exists(instructionsPath))
            return null;

        try
        {
            return File.ReadAllText(instructionsPath);
        }
        catch
        {
            return null;
        }
    }

    // ─── read_github_file ─────────────────────────────────────────────────────

    private sealed class ReadGithubFileTool(string githubRoot) : IAgentTool
    {
        public string Name => "read_github_file";

        public string Description =>
            "Читает файл из папки .github (workflows, шаблоны PR, copilot-instructions.md и т.п.). " +
            "Доступ ограничен только папкой .github. " +
            "Используй для чтения правил CI/CD, шаблонов pull request, инструкций copilot.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                file_path = new
                {
                    type = "string",
                    description = "Относительный путь к файлу внутри .github. " +
                                  "Пример: 'copilot-instructions.md' или 'workflows/ci.yml' или 'PULL_REQUEST_TEMPLATE.md'"
                }
            },
            required = new[] { "file_path" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("file_path", out var pathEl))
                return Task.FromResult("Ошибка: не передан параметр file_path");

            var relativePath = pathEl.GetString()?.Trim().Trim('"', '\'') ?? "";
            if (string.IsNullOrEmpty(relativePath))
                return Task.FromResult("Ошибка: file_path пустой");

            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(githubRoot, relativePath));

                // Защита: путь должен быть внутри .github
                var githubRootNormalized = githubRoot.TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(githubRootNormalized, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult($"⛔ Доступ запрещён: путь '{relativePath}' выходит за пределы папки .github");

                if (!File.Exists(fullPath))
                    return Task.FromResult($"❌ Файл не найден: .github/{relativePath}");

                var content = File.ReadAllText(fullPath);
                return Task.FromResult($"Содержимое .github/{relativePath}:\n\n{content}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка чтения файла .github/{relativePath}: {ex.Message}");
            }
        }
    }

    // ─── list_github_files ────────────────────────────────────────────────────

    private sealed class ListGithubFilesTool(string githubRoot) : IAgentTool
    {
        public string Name => "list_github_files";

        public string Description =>
            "Показывает список файлов в папке .github (workflows, шаблоны, инструкции). " +
            "Используй чтобы узнать какие файлы доступны перед чтением через read_github_file.";

        public object Parameters => new
        {
            type = "object",
            properties = new { },
            required = Array.Empty<string>()
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Содержимое папки .github ({githubRoot}):");
                sb.AppendLine();
                AppendDirectory(sb, githubRoot, githubRoot, "");
                return Task.FromResult(sb.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка чтения папки .github: {ex.Message}");
            }
        }

        private static void AppendDirectory(StringBuilder sb, string basePath, string dir, string indent)
        {
            foreach (var subDir in Directory.GetDirectories(dir).OrderBy(d => d))
            {
                var name = Path.GetFileName(subDir);
                sb.AppendLine($"{indent}📁 {name}/");
                AppendDirectory(sb, basePath, subDir, indent + "  ");
            }
            foreach (var file in Directory.GetFiles(dir).OrderBy(f => f))
            {
                var relativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');
                sb.AppendLine($"{indent}📄 {relativePath}");
            }
        }
    }
}

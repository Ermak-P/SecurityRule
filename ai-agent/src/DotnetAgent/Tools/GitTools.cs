using System.Text;
using System.Text.Json;
using LibGit2Sharp;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для работы с Git через LibGit2Sharp.
///
/// Фаза 4: показывать diff изменений, создавать коммиты, просматривать историю.
///
/// Доступные инструменты:
///   - git_status  — статус рабочей директории (изменённые, новые, удалённые файлы)
///   - git_diff    — показывает diff изменений (весь или по файлу)
///   - git_commit  — создаёт коммит с текущими изменениями
///   - git_log     — история коммитов
/// </summary>
public static class GitTools
{
    /// <summary>Создаёт инструменты Git. Возвращает пустой список если нет .git.</summary>
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        // Ищем .git директорию вверх по дереву директорий
        var gitRoot = FindGitRoot(projectPath);
        if (gitRoot == null)
            return Array.Empty<IAgentTool>();

        return new IAgentTool[]
        {
            new GitStatusTool(gitRoot),
            new GitDiffTool(gitRoot),
            new GitCommitTool(gitRoot),
            new GitLogTool(gitRoot),
        };
    }

    private static string? FindGitRoot(string startPath)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;
            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }
        return null;
    }

    // ─── git_status ───────────────────────────────────────────────────────────

    private sealed class GitStatusTool : IAgentTool
    {
        private readonly string _gitRoot;

        public GitStatusTool(string gitRoot) => _gitRoot = gitRoot;

        public string Name => "git_status";

        public string Description =>
            "Показывает статус рабочей директории Git: изменённые, добавленные, удалённые файлы. " +
            "Аналог 'git status'.";

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
                using var repo = new Repository(_gitRoot);
                var status = repo.RetrieveStatus(new StatusOptions());

                if (!status.IsDirty)
                    return Task.FromResult("✅ Нет изменений. Рабочая директория чистая.");

                var sb = new StringBuilder();
                sb.AppendLine($"Ветка: {repo.Head.FriendlyName}");
                sb.AppendLine();

                var staged = status.Staged.ToList();
                if (staged.Count > 0)
                {
                    sb.AppendLine("Проиндексированные изменения (staged):");
                    foreach (var e in staged)
                        sb.AppendLine($"  {StatusIcon(e.State)} {e.FilePath}");
                }

                var unstaged = status.Modified.Concat(status.Missing).ToList();
                if (unstaged.Count > 0)
                {
                    sb.AppendLine("Не проиндексированные изменения:");
                    foreach (var e in unstaged)
                        sb.AppendLine($"  {StatusIcon(e.State)} {e.FilePath}");
                }

                var untracked = status.Untracked.ToList();
                if (untracked.Count > 0)
                {
                    sb.AppendLine("Неотслеживаемые файлы:");
                    foreach (var e in untracked)
                        sb.AppendLine($"  ? {e.FilePath}");
                }

                return Task.FromResult(sb.ToString().TrimEnd());
            }
            catch (RepositoryNotFoundException)
            {
                return Task.FromResult($"Git репозиторий не найден по пути: {_gitRoot}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка git status: {ex.Message}");
            }
        }

        private static string StatusIcon(FileStatus state) => state switch
        {
            FileStatus.NewInIndex => "A",
            FileStatus.ModifiedInIndex => "M",
            FileStatus.DeletedFromIndex => "D",
            FileStatus.ModifiedInWorkdir => "M",
            FileStatus.DeletedFromWorkdir => "D",
            _ => "?"
        };
    }

    // ─── git_diff ─────────────────────────────────────────────────────────────

    private sealed class GitDiffTool : IAgentTool
    {
        private readonly string _gitRoot;

        public GitDiffTool(string gitRoot) => _gitRoot = gitRoot;

        public string Name => "git_diff";

        public string Description =>
            "Показывает diff изменений в рабочей директории. " +
            "Можно указать конкретный файл или получить весь diff.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                file_path = new
                {
                    type = "string",
                    description = "Относительный путь к файлу. Если не указан — показывает весь diff."
                }
            },
            required = Array.Empty<string>()
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            try
            {
                using var repo = new Repository(_gitRoot);

                string? filePath = null;
                if (arguments.TryGetProperty("file_path", out var pathEl))
                    filePath = pathEl.GetString()?.Trim().Trim('"', '\'');

                Patch diff;

                if (!string.IsNullOrEmpty(filePath))
                {
                    diff = repo.Diff.Compare<Patch>(
                        repo.Head.Tip?.Tree,
                        DiffTargets.WorkingDirectory,
                        new[] { filePath });
                }
                else
                {
                    diff = repo.Diff.Compare<Patch>(
                        repo.Head.Tip?.Tree,
                        DiffTargets.WorkingDirectory);
                }

                if (!diff.Any())
                    return Task.FromResult("Нет изменений.");

                var sb = new StringBuilder();
                const int maxLines = 500;
                var lineCount = 0;

                foreach (var entry in diff)
                {
                    sb.AppendLine($"--- {entry.OldPath}");
                    sb.AppendLine($"+++ {entry.Path}");
                    foreach (var line in entry.Patch.Split('\n'))
                    {
                        sb.AppendLine(line);
                        if (++lineCount >= maxLines)
                        {
                            sb.AppendLine($"\n... (truncated, {diff.LinesAdded + diff.LinesDeleted} total lines)");
                            return Task.FromResult(sb.ToString().TrimEnd());
                        }
                    }
                }

                return Task.FromResult(sb.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка git diff: {ex.Message}");
            }
        }
    }

    // ─── git_commit ───────────────────────────────────────────────────────────

    private sealed class GitCommitTool : IAgentTool
    {
        private readonly string _gitRoot;

        public GitCommitTool(string gitRoot) => _gitRoot = gitRoot;

        public string Name => "git_commit";

        public string Description =>
            "Создаёт Git коммит. Сначала добавляет все изменения (git add -A), " +
            "затем создаёт коммит с указанным сообщением.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                message = new { type = "string", description = "Сообщение коммита" }
            },
            required = new[] { "message" }
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("message", out var msgEl))
                return Task.FromResult("Ошибка: не передан параметр message");

            var message = msgEl.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(message))
                return Task.FromResult("Ошибка: message пустой");

            try
            {
                using var repo = new Repository(_gitRoot);

                // Добавляем все изменения
                Commands.Stage(repo, "*");

                var status = repo.RetrieveStatus();
                if (!status.IsDirty)
                    return Task.FromResult("Нет изменений для коммита.");

                // Получаем информацию об авторе из git config
                var config = repo.Config;
                var authorName = config.GetValueOrDefault("user.name", "AI Agent");
                var authorEmail = config.GetValueOrDefault("user.email", "agent@localhost");

                var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
                var committer = author;

                var commit = repo.Commit(message, author, committer);

                return Task.FromResult(
                    $"✅ Коммит создан: {commit.Sha[..8]} — {message}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка git commit: {ex.Message}");
            }
        }
    }

    // ─── git_log ──────────────────────────────────────────────────────────────

    private sealed class GitLogTool : IAgentTool
    {
        private readonly string _gitRoot;

        public GitLogTool(string gitRoot) => _gitRoot = gitRoot;

        public string Name => "git_log";

        public string Description =>
            "Показывает историю коммитов (последние N коммитов).";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                limit = new
                {
                    type = "integer",
                    description = "Количество последних коммитов (по умолчанию 10)"
                }
            },
            required = Array.Empty<string>()
        };

        public Task<string> ExecuteAsync(JsonElement arguments)
        {
            var limit = 10;
            if (arguments.TryGetProperty("limit", out var limitEl) && limitEl.TryGetInt32(out var l))
                limit = Math.Clamp(l, 1, 100);

            try
            {
                using var repo = new Repository(_gitRoot);
                var sb = new StringBuilder();
                sb.AppendLine($"Ветка: {repo.Head.FriendlyName}");
                sb.AppendLine();

                var commits = repo.Commits.Take(limit);
                foreach (var commit in commits)
                {
                    var date = commit.Author.When.ToString("yyyy-MM-dd HH:mm");
                    sb.AppendLine($"{commit.Sha[..8]}  {date}  {commit.Author.Name}");
                    sb.AppendLine($"         {commit.MessageShort}");
                }

                return Task.FromResult(sb.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка git log: {ex.Message}");
            }
        }
    }
}

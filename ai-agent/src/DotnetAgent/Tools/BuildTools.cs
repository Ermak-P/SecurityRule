using System.Text.Json;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для сборки и тестирования .NET проекта.
///
/// Фаза 2: запускать dotnet build после изменений и передавать ошибки компилятора обратно в LLM.
/// Фаза 4: запускать dotnet test и анализировать упавшие тесты.
/// </summary>
public static class BuildTools
{
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        return new IAgentTool[]
        {
            new DotnetBuildTool(normalizedPath),
            new DotnetTestTool(normalizedPath),
        };
    }

    // ─── dotnet_build ─────────────────────────────────────────────────────────

    private sealed class DotnetBuildTool : IAgentTool
    {
        private readonly string _projectPath;

        public DotnetBuildTool(string projectPath) => _projectPath = projectPath;

        public string Name => "dotnet_build";

        public string Description =>
            "Запускает 'dotnet build' в директории проекта. Возвращает вывод компилятора: " +
            "предупреждения, ошибки, статус сборки. Используй после изменений файлов чтобы убедиться " +
            "что код компилируется.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                project_path = new
                {
                    type = "string",
                    description = "Относительный путь до .csproj файла или директории. " +
                                  "Если не указан — собирает весь проект."
                }
            },
            required = Array.Empty<string>()
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            var subPath = "";
            if (arguments.TryGetProperty("project_path", out var pathEl))
                subPath = pathEl.GetString()?.Trim().Trim('"', '\'') ?? "";

            var buildTarget = string.IsNullOrEmpty(subPath)
                ? _projectPath
                : Path.GetFullPath(Path.Combine(_projectPath, subPath));

            return await RunDotnetAsync("build", buildTarget,
                "--no-incremental", "-v", "minimal");
        }
    }

    // ─── dotnet_test ──────────────────────────────────────────────────────────

    private sealed class DotnetTestTool : IAgentTool
    {
        private readonly string _projectPath;

        public DotnetTestTool(string projectPath) => _projectPath = projectPath;

        public string Name => "dotnet_test";

        public string Description =>
            "Запускает 'dotnet test' в директории проекта. Возвращает результаты тестирования: " +
            "пройденные, упавшие тесты, трассировки ошибок.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                project_path = new
                {
                    type = "string",
                    description = "Относительный путь до тестового .csproj файла или директории."
                },
                filter = new
                {
                    type = "string",
                    description = "Фильтр тестов (например: 'ClassName=MyTests' или 'Category=Unit')"
                }
            },
            required = Array.Empty<string>()
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            var subPath = "";
            if (arguments.TryGetProperty("project_path", out var pathEl))
                subPath = pathEl.GetString()?.Trim().Trim('"', '\'') ?? "";

            var testTarget = string.IsNullOrEmpty(subPath)
                ? _projectPath
                : Path.GetFullPath(Path.Combine(_projectPath, subPath));

            var extraArgs = new List<string>();
            if (arguments.TryGetProperty("filter", out var filterEl))
            {
                var filter = filterEl.GetString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(filter))
                {
                    extraArgs.Add("--filter");
                    extraArgs.Add(filter);
                }
            }

            return await RunDotnetAsync("test", testTarget,
                ["-v", "normal", .. extraArgs]);
        }
    }

    // ─── Общий запуск dotnet ──────────────────────────────────────────────────

    private static async Task<string> RunDotnetAsync(string command, string workingDir, params string[] args)
    {
        var allArgs = new List<string> { command };
        allArgs.AddRange(args);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Directory.Exists(workingDir) ? workingDir : Path.GetDirectoryName(workingDir) ?? workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in allArgs)
            psi.ArgumentList.Add(arg);

        // Для .csproj файла передаём его явно
        if (File.Exists(workingDir))
        {
            psi.WorkingDirectory = Path.GetDirectoryName(workingDir)!;
            psi.ArgumentList.Add(workingDir);
        }

        using var process = new System.Diagnostics.Process { StartInfo = psi };

        var outputLines = new System.Collections.Concurrent.ConcurrentBag<string>();
        var errorLines = new System.Collections.Concurrent.ConcurrentBag<string>();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputLines.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorLines.Add(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Таймаут 2 минуты на сборку
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return "❌ Ошибка: dotnet build превысил таймаут 2 минуты.";
        }

        var output = string.Join("\n", outputLines);
        var error = string.Join("\n", errorLines);
        var combined = string.Join("\n", new[] { output, error }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var exitCode = process.ExitCode;
        var status = exitCode == 0 ? "✅ Сборка успешна" : $"❌ Сборка завершилась с ошибкой (код {exitCode})";

        return $"{status}\n\n{combined}".Trim();
    }
}

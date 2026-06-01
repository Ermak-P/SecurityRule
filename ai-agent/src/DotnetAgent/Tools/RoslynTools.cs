using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для анализа C# кода с помощью Roslyn (AST-анализ).
///
/// Фаза 2: понимание структуры .NET проектов на уровне кода.
///
/// Доступные инструменты:
///   - get_class_info    — поля, свойства, методы класса
///   - get_usages        — где используется класс или метод
///   - get_dependencies  — зависимости через using / DI
///   - patch_method      — заменить тело одного метода без перезаписи файла
/// </summary>
public static class RoslynTools
{
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        return new IAgentTool[]
        {
            new GetClassInfoTool(normalizedPath),
            new GetUsagesTool(normalizedPath),
            new GetDependenciesTool(normalizedPath),
            new PatchMethodTool(normalizedPath),
        };
    }

    // ─── Вспомогательные методы ────────────────────────────────────────────────

    /// <summary>Загружает синтаксическое дерево Roslyn для файла.</summary>
    private static SyntaxTree ParseFile(string filePath)
    {
        var code = File.ReadAllText(filePath, Encoding.UTF8);
        return CSharpSyntaxTree.ParseText(code, path: filePath);
    }

    /// <summary>Рекурсивно находит все .cs файлы проекта (исключая bin/obj).</summary>
    internal static IEnumerable<string> FindCsFiles(string root)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "bin", "obj", ".git", ".vs", ".idea", "node_modules" };

        return EnumerateCs(root);

        IEnumerable<string> EnumerateCs(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
                yield return file;

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                if (!skip.Contains(Path.GetFileName(sub)))
                    foreach (var f in EnumerateCs(sub))
                        yield return f;
            }
        }
    }

    // ─── get_class_info ───────────────────────────────────────────────────────

    private sealed class GetClassInfoTool : IAgentTool
    {
        private readonly string _projectPath;

        public GetClassInfoTool(string projectPath) => _projectPath = projectPath;

        public string Name => "get_class_info";

        public string Description =>
            "Возвращает структуру C# класса: поля, свойства, методы (с сигнатурами). " +
            "Передай имя класса (без namespace). Если классов с таким именем несколько — покажет все.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                class_name = new { type = "string", description = "Имя класса (например: UserService, IRepository)" }
            },
            required = new[] { "class_name" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("class_name", out var nameEl))
                return "Ошибка: не передан параметр class_name";

            var className = nameEl.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(className))
                return "Ошибка: class_name пустой";

            var sb = new StringBuilder();
            var found = false;

            foreach (var file in FindCsFiles(_projectPath))
            {
                var tree = ParseFile(file);
                var root = await tree.GetRootAsync();

                var classes = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase));

                foreach (var cls in classes)
                {
                    found = true;
                    var rel = Path.GetRelativePath(_projectPath, file);
                    sb.AppendLine($"=== {cls.Identifier.Text} ({rel}) ===");

                    // Базовые типы / интерфейсы
                    if (cls.BaseList?.Types.Count > 0)
                    {
                        var bases = string.Join(", ", cls.BaseList.Types.Select(t => t.ToString()));
                        sb.AppendLine($"  Наследует/реализует: {bases}");
                    }

                    // Поля
                    var fields = cls.Members.OfType<FieldDeclarationSyntax>().ToList();
                    if (fields.Count > 0)
                    {
                        sb.AppendLine("  Поля:");
                        foreach (var f in fields)
                        {
                            var mods = string.Join(" ", f.Modifiers);
                            var varNames = string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.Text));
                            sb.AppendLine($"    {mods} {f.Declaration.Type} {varNames}");
                        }
                    }

                    // Свойства
                    var props = cls.Members.OfType<PropertyDeclarationSyntax>().ToList();
                    if (props.Count > 0)
                    {
                        sb.AppendLine("  Свойства:");
                        foreach (var p in props)
                        {
                            var mods = string.Join(" ", p.Modifiers);
                            sb.AppendLine($"    {mods} {p.Type} {p.Identifier.Text}");
                        }
                    }

                    // Методы
                    var methods = cls.Members.OfType<MethodDeclarationSyntax>().ToList();
                    if (methods.Count > 0)
                    {
                        sb.AppendLine("  Методы:");
                        foreach (var m in methods)
                        {
                            var mods = string.Join(" ", m.Modifiers);
                            var parms = string.Join(", ", m.ParameterList.Parameters.Select(p =>
                                $"{p.Type} {p.Identifier.Text}"));
                            sb.AppendLine($"    {mods} {m.ReturnType} {m.Identifier.Text}({parms})");
                        }
                    }

                    // Конструкторы
                    var ctors = cls.Members.OfType<ConstructorDeclarationSyntax>().ToList();
                    if (ctors.Count > 0)
                    {
                        sb.AppendLine("  Конструкторы:");
                        foreach (var c in ctors)
                        {
                            var parms = string.Join(", ", c.ParameterList.Parameters.Select(p =>
                                $"{p.Type} {p.Identifier.Text}"));
                            sb.AppendLine($"    {c.Identifier.Text}({parms})");
                        }
                    }

                    sb.AppendLine();
                }
            }

            return found ? sb.ToString().TrimEnd()
                : $"Класс '{className}' не найден в проекте.";
        }
    }

    // ─── get_usages ───────────────────────────────────────────────────────────

    private sealed class GetUsagesTool : IAgentTool
    {
        private readonly string _projectPath;

        public GetUsagesTool(string projectPath) => _projectPath = projectPath;

        public string Name => "get_usages";

        public string Description =>
            "Ищет упоминания (usages) класса, метода или переменной по имени во всех .cs файлах. " +
            "Возвращает список файлов и номеров строк где встречается указанное имя.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                symbol_name = new { type = "string", description = "Имя символа для поиска (класс, метод, переменная)" }
            },
            required = new[] { "symbol_name" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("symbol_name", out var nameEl))
                return "Ошибка: не передан параметр symbol_name";

            var symbol = nameEl.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(symbol))
                return "Ошибка: symbol_name пустой";

            var sb = new StringBuilder();
            var totalCount = 0;

            foreach (var file in FindCsFiles(_projectPath))
            {
                var tree = ParseFile(file);
                var root = await tree.GetRootAsync();

                var hits = root.DescendantTokens()
                    .Where(t => t.IsKind(SyntaxKind.IdentifierToken)
                             && t.Text.Equals(symbol, StringComparison.Ordinal))
                    .ToList();

                if (hits.Count == 0) continue;

                var rel = Path.GetRelativePath(_projectPath, file);
                sb.AppendLine($"{rel}:");

                var text = root.GetText();
                foreach (var token in hits)
                {
                    var line = text.Lines.GetLineFromPosition(token.SpanStart);
                    var lineNumber = line.LineNumber + 1;
                    var lineText = line.ToString().Trim();
                    sb.AppendLine($"  [{lineNumber}] {lineText}");
                    totalCount++;
                }
            }

            if (totalCount == 0)
                return $"Символ '{symbol}' не найден ни в одном .cs файле.";

            sb.Insert(0, $"Найдено {totalCount} упоминаний символа '{symbol}':\n\n");
            return sb.ToString().TrimEnd();
        }
    }

    // ─── get_dependencies ─────────────────────────────────────────────────────

    private sealed class GetDependenciesTool : IAgentTool
    {
        private readonly string _projectPath;

        public GetDependenciesTool(string projectPath) => _projectPath = projectPath;

        public string Name => "get_dependencies";

        public string Description =>
            "Анализирует зависимости класса: параметры конструктора (DI), using-директивы, " +
            "а также зависимости проектов из .csproj файлов.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                class_name = new
                {
                    type = "string",
                    description = "Имя класса для анализа зависимостей. " +
                                  "Если не указан — показывает зависимости проектов из .csproj."
                }
            },
            required = Array.Empty<string>()
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            arguments.TryGetProperty("class_name", out var nameEl);
            var className = nameEl.GetString()?.Trim() ?? "";

            var sb = new StringBuilder();

            // Зависимости .csproj файлов
            var csprojFiles = Directory.GetFiles(_projectPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length > 0)
            {
                sb.AppendLine("=== Зависимости проектов (.csproj) ===");
                foreach (var csproj in csprojFiles)
                {
                    var rel = Path.GetRelativePath(_projectPath, csproj);
                    sb.AppendLine($"\n{rel}:");
                    var content = await File.ReadAllTextAsync(csproj);
                    // Извлекаем PackageReference
                    var doc = System.Xml.Linq.XDocument.Parse(content);
                    var pkgRefs = doc.Descendants("PackageReference")
                        .Select(e => $"  📦 {e.Attribute("Include")?.Value} {e.Attribute("Version")?.Value}");
                    var projRefs = doc.Descendants("ProjectReference")
                        .Select(e => $"  🔗 {e.Attribute("Include")?.Value}");
                    foreach (var r in pkgRefs.Concat(projRefs))
                        sb.AppendLine(r);
                }
            }

            if (string.IsNullOrEmpty(className))
                return sb.ToString().TrimEnd();

            // Зависимости конкретного класса
            sb.AppendLine($"\n=== Зависимости класса {className} ===");
            var found = false;

            foreach (var file in FindCsFiles(_projectPath))
            {
                var tree = ParseFile(file);
                var root = await tree.GetRootAsync();

                var classes = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase));

                foreach (var cls in classes)
                {
                    found = true;
                    var rel = Path.GetRelativePath(_projectPath, file);
                    sb.AppendLine($"\nФайл: {rel}");

                    // Using-директивы файла
                    var usings = root.DescendantNodes()
                        .OfType<UsingDirectiveSyntax>()
                        .Select(u => u.Name?.ToString() ?? "")
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();
                    if (usings.Count > 0)
                    {
                        sb.AppendLine("  Using:");
                        foreach (var u in usings)
                            sb.AppendLine($"    {u}");
                    }

                    // DI зависимости (параметры конструктора)
                    var ctors = cls.Members.OfType<ConstructorDeclarationSyntax>().ToList();
                    foreach (var ctor in ctors)
                    {
                        var deps = ctor.ParameterList.Parameters
                            .Select(p => $"{p.Type} {p.Identifier.Text}")
                            .ToList();
                        if (deps.Count > 0)
                        {
                            sb.AppendLine("  DI-зависимости (конструктор):");
                            foreach (var d in deps)
                                sb.AppendLine($"    {d}");
                        }
                    }
                }
            }

            if (!found && !string.IsNullOrEmpty(className))
                sb.AppendLine($"Класс '{className}' не найден.");

            return sb.ToString().TrimEnd();
        }
    }

    // ─── patch_method ─────────────────────────────────────────────────────────

    private sealed class PatchMethodTool : IAgentTool
    {
        private readonly string _projectPath;

        public PatchMethodTool(string projectPath) => _projectPath = projectPath;

        public string Name => "patch_method";

        public string Description =>
            "Заменяет тело одного метода в C# файле. Принимает имя класса, имя метода и новое тело метода. " +
            "Не перезаписывает весь файл — изменяет только указанный метод. " +
            "new_body должен содержать только содержимое фигурных скобок (без самих скобок { }).";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                file_path = new { type = "string", description = "Относительный путь к .cs файлу" },
                class_name = new { type = "string", description = "Имя класса" },
                method_name = new { type = "string", description = "Имя метода" },
                new_body = new { type = "string", description = "Новое тело метода (без фигурных скобок)" }
            },
            required = new[] { "file_path", "class_name", "method_name", "new_body" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("file_path", out var pathEl) ||
                !arguments.TryGetProperty("class_name", out var classEl) ||
                !arguments.TryGetProperty("method_name", out var methodEl) ||
                !arguments.TryGetProperty("new_body", out var bodyEl))
                return "Ошибка: не все параметры переданы (file_path, class_name, method_name, new_body)";

            var relPath = pathEl.GetString()?.Trim() ?? "";
            var className = classEl.GetString()?.Trim() ?? "";
            var methodName = methodEl.GetString()?.Trim() ?? "";
            var newBody = bodyEl.GetString() ?? "";

            // Безопасный путь
            relPath = relPath.Trim('"', '\'');
            var fullPath = Path.IsPathRooted(relPath)
                ? Path.GetFullPath(relPath)
                : Path.GetFullPath(Path.Combine(_projectPath, relPath));

            var projectRoot = _projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return $"Ошибка: путь '{relPath}' выходит за пределы проекта.";

            if (!File.Exists(fullPath))
                return $"Ошибка: файл '{relPath}' не найден.";

            var originalCode = await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
            var tree = CSharpSyntaxTree.ParseText(originalCode, path: fullPath);
            var root = await tree.GetRootAsync();

            // Ищем метод в указанном классе
            var targetMethod = root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Where(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase))
                .SelectMany(c => c.Members.OfType<MethodDeclarationSyntax>())
                .FirstOrDefault(m => m.Identifier.Text.Equals(methodName, StringComparison.OrdinalIgnoreCase));

            if (targetMethod == null)
                return $"Ошибка: метод '{methodName}' не найден в классе '{className}' файла '{relPath}'.";

            if (targetMethod.Body == null)
                return $"Ошибка: метод '{methodName}' является expression-bodied или abstract — patch_method работает только с block-body методами.";

            // Формируем новое тело
            var newBodyStatement = $"{{\n{newBody}\n}}";
            var newBodyBlock = SyntaxFactory.ParseStatement(newBodyStatement) as BlockSyntax;
            if (newBodyBlock == null)
                return $"Ошибка: не удалось разобрать new_body как корректный C# блок.";

            // Заменяем тело метода
            var newMethod = targetMethod.WithBody(newBodyBlock
                .WithLeadingTrivia(targetMethod.Body.GetLeadingTrivia())
                .WithTrailingTrivia(targetMethod.Body.GetTrailingTrivia()));

            var newRoot = root.ReplaceNode(targetMethod, newMethod);
            var newCode = newRoot.ToFullString();

            await File.WriteAllTextAsync(fullPath, newCode, Encoding.UTF8);

            return $"✓ Метод '{className}.{methodName}' успешно обновлён в '{relPath}'.";
        }
    }
}

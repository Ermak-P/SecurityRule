using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetAgent.Tools;

/// <summary>
/// Инструменты для автоматической генерации тестов.
///
/// Фаза 5: автогенерация тестов для новых методов.
///
/// Инструменты:
///   - generate_tests — генерирует заготовку xUnit-тестов для класса или метода
/// </summary>
public static class TestGenerationTools
{
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        return new IAgentTool[]
        {
            new GenerateTestsTool(normalizedPath),
        };
    }

    // ─── generate_tests ───────────────────────────────────────────────────────

    private sealed class GenerateTestsTool : IAgentTool
    {
        private readonly string _projectPath;

        public GenerateTestsTool(string projectPath) => _projectPath = projectPath;

        public string Name => "generate_tests";

        public string Description =>
            "Генерирует заготовку xUnit-тестов для указанного класса. " +
            "Создаёт файл с тест-классом, по одному тесту на каждый публичный метод. " +
            "Тесты помечены [Fact] и содержат // Arrange, // Act, // Assert.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                class_name = new { type = "string", description = "Имя класса для которого генерировать тесты" },
                output_path = new
                {
                    type = "string",
                    description = "Путь для сохранения тестового файла (относительно проекта). " +
                                  "Если не указан — выводит в консоль без сохранения."
                }
            },
            required = new[] { "class_name" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("class_name", out var classEl))
                return "Ошибка: не передан параметр class_name";

            var className = classEl.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(className))
                return "Ошибка: class_name пустой";

            string? outputPath = null;
            if (arguments.TryGetProperty("output_path", out var outEl))
                outputPath = outEl.GetString()?.Trim().Trim('"', '\'');

            // Ищем класс в проекте
            TypeDeclarationSyntax? foundClass = null;
            string? foundNamespace = null;

            foreach (var file in RoslynTools.FindCsFiles(_projectPath))
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var cls = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text.Equals(className,
                        StringComparison.OrdinalIgnoreCase));

                if (cls == null) continue;

                foundClass = cls;
                // Получаем namespace
                foundNamespace = root.DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString()
                    ?? root.DescendantNodes()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString();
                break;
            }

            if (foundClass == null)
                return $"Класс '{className}' не найден в проекте.";

            // Получаем публичные методы
            var publicMethods = foundClass.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod =>
                    mod.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToList();

            // Генерируем тест-файл
            var sb = new StringBuilder();
            var testNamespace = foundNamespace != null
                ? $"{foundNamespace}.Tests"
                : "Tests";

            sb.AppendLine("using Xunit;");
            sb.AppendLine();
            sb.AppendLine($"namespace {testNamespace};");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// Тесты для <see cref=\"{className}\"/>.");
            sb.AppendLine($"/// Сгенерировано AI агентом — заполните тела тестов.");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public class {className}Tests");
            sb.AppendLine("{");

            // Поля класса
            sb.AppendLine($"    private readonly {className} _sut;");
            sb.AppendLine();
            sb.AppendLine($"    public {className}Tests()");
            sb.AppendLine("    {");
            sb.AppendLine($"        // TODO: инициализируй _sut с нужными зависимостями");
            sb.AppendLine($"        // _sut = new {className}(...);");
            sb.AppendLine("    }");

            // Тест для каждого метода
            foreach (var method in publicMethods)
            {
                var methodName = method.Identifier.Text;
                var returnType = method.ReturnType.ToString();
                var parms = method.ParameterList.Parameters;

                sb.AppendLine();
                sb.AppendLine("    [Fact]");

                // Асинхронный тест если метод возвращает Task
                if (returnType.StartsWith("Task") || returnType.StartsWith("ValueTask"))
                {
                    sb.AppendLine($"    public async Task {methodName}_ShouldWork()");
                    sb.AppendLine("    {");
                    sb.AppendLine("        // Arrange");
                    foreach (var p in parms)
                        sb.AppendLine($"        {p.Type} {p.Identifier.Text} = default!;");
                    sb.AppendLine();
                    sb.AppendLine("        // Act");
                    var awaitKeyword = returnType == "Task" || returnType == "ValueTask" ? "await " : "var result = await ";
                    var argsStr = string.Join(", ", parms.Select(p => p.Identifier.Text));
                    sb.AppendLine($"        {awaitKeyword}_sut.{methodName}({argsStr});");
                    sb.AppendLine();
                    sb.AppendLine("        // Assert");
                    sb.AppendLine("        Assert.True(true); // TODO: добавь assertions");
                }
                else
                {
                    sb.AppendLine($"    public void {methodName}_ShouldWork()");
                    sb.AppendLine("    {");
                    sb.AppendLine("        // Arrange");
                    foreach (var p in parms)
                        sb.AppendLine($"        {p.Type} {p.Identifier.Text} = default!;");
                    sb.AppendLine();
                    sb.AppendLine("        // Act");
                    var argsStr = string.Join(", ", parms.Select(p => p.Identifier.Text));
                    if (returnType == "void")
                        sb.AppendLine($"        _sut.{methodName}({argsStr});");
                    else
                        sb.AppendLine($"        var result = _sut.{methodName}({argsStr});");
                    sb.AppendLine();
                    sb.AppendLine("        // Assert");
                    sb.AppendLine("        Assert.True(true); // TODO: добавь assertions");
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            var generatedCode = sb.ToString();

            // Сохраняем файл если указан путь
            if (!string.IsNullOrEmpty(outputPath))
            {
                var fullPath = Path.IsPathRooted(outputPath)
                    ? Path.GetFullPath(outputPath)
                    : Path.GetFullPath(Path.Combine(_projectPath, outputPath));

                var projectRoot = _projectPath.TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                    return $"Ошибка: путь '{outputPath}' выходит за пределы проекта.";

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(fullPath, generatedCode, Encoding.UTF8);
                return $"✅ Тестовый файл создан: {Path.GetRelativePath(_projectPath, fullPath)}\n\n" +
                       $"Методов охвачено: {publicMethods.Count}";
            }

            return $"Сгенерированные тесты для {className} " +
                   $"({publicMethods.Count} методов):\n\n```csharp\n{generatedCode}\n```";
        }
    }
}

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
///   - generate_tests       — генерирует заготовку NUnit-тестов для класса или метода
///   - generate_feature     — генерирует SpecFlow .feature файл (Gherkin, русский язык)
///
/// Соглашения тестирования проекта:
///   - Unit/Integration: NUnit [Test], имена Returns_X_When_Y
///   - E2E: SpecFlow .feature на русском языке + StepDefinitions/ с data-testid
/// </summary>
public static class TestGenerationTools
{
    public static IEnumerable<IAgentTool> Create(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        return new IAgentTool[]
        {
            new GenerateTestsTool(normalizedPath),
            new GenerateFeatureTool(normalizedPath),
        };
    }

    // ─── generate_tests ───────────────────────────────────────────────────────

    private sealed class GenerateTestsTool : IAgentTool
    {
        private readonly string _projectPath;

        public GenerateTestsTool(string projectPath) => _projectPath = projectPath;

        public string Name => "generate_tests";

        public string Description =>
            "Генерирует заготовку NUnit-тестов для указанного класса в соответствии с соглашениями проекта. " +
            "Создаёт файл с тест-классом, по одному тесту на каждый публичный метод. " +
            "Unit-тесты используют [Test] и паттерн Returns_X_When_Y. " +
            "Integration-тесты для репозиториев используют InMemory AppDbContext.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                class_name = new { type = "string", description = "Имя класса для которого генерировать тесты" },
                test_type = new
                {
                    type = "string",
                    description = "Тип тестов: 'unit' (по умолчанию), 'integration' (для репозиториев с InMemory EF)",
                    @enum = new[] { "unit", "integration" }
                },
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

            var testType = "unit";
            if (arguments.TryGetProperty("test_type", out var typeEl))
                testType = typeEl.GetString()?.ToLowerInvariant() ?? "unit";

            string? outputPath = null;
            if (arguments.TryGetProperty("output_path", out var outEl))
                outputPath = outEl.GetString()?.Trim().Trim('"', '\'');

            // Ищем класс в проекте
            TypeDeclarationSyntax? foundClass = null;
            string? foundNamespace = null;
            string? foundFilePath = null;

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
                foundFilePath = file;
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

            // Определяем слой архитектуры по пути файла
            var layer = DetectLayer(foundFilePath ?? "");

            // Принудительно integration для репозиториев
            if (layer == "infrastructure" && className.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
                testType = "integration";

            // Получаем публичные методы
            var publicMethods = foundClass.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod =>
                    mod.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToList();

            var generatedCode = testType == "integration"
                ? GenerateIntegrationTests(className, foundNamespace, publicMethods)
                : GenerateUnitTests(className, foundNamespace, publicMethods);

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
                       $"Тип: {testType} | Методов охвачено: {publicMethods.Count}";
            }

            return $"Сгенерированные {testType}-тесты для {className} " +
                   $"({publicMethods.Count} методов):\n\n```csharp\n{generatedCode}\n```";
        }

        /// <summary>Генерирует NUnit unit-тесты с моками зависимостей.</summary>
        private static string GenerateUnitTests(
            string className,
            string? foundNamespace,
            List<MethodDeclarationSyntax> publicMethods)
        {
            var sb = new StringBuilder();
            var testNamespace = foundNamespace != null
                ? $"{foundNamespace}.Tests"
                : "Tests";

            sb.AppendLine("using NUnit.Framework;");
            sb.AppendLine("using NSubstitute;");
            sb.AppendLine();
            sb.AppendLine($"namespace {testNamespace};");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>Unit-тесты для <see cref=\"{className}\"/>.</summary>");
            sb.AppendLine($"/// <remarks>Сгенерировано AI агентом — заполните тела тестов.</remarks>");
            sb.AppendLine("[TestFixture]");
            sb.AppendLine($"public class {className}Tests");
            sb.AppendLine("{");
            sb.AppendLine($"    private {className} _sut = null!;");
            sb.AppendLine();
            sb.AppendLine("    [SetUp]");
            sb.AppendLine("    public void SetUp()");
            sb.AppendLine("    {");
            sb.AppendLine($"        // TODO: инициализируй _sut с нужными зависимостями");
            sb.AppendLine($"        // Пример: _sut = new {className}(Substitute.For<IDependency>());");
            sb.AppendLine("    }");

            foreach (var method in publicMethods)
                AppendNUnitTest(sb, method, async: IsAsync(method));

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>Генерирует NUnit integration-тесты с InMemory AppDbContext.</summary>
        private static string GenerateIntegrationTests(
            string className,
            string? foundNamespace,
            List<MethodDeclarationSyntax> publicMethods)
        {
            var sb = new StringBuilder();
            var testNamespace = foundNamespace != null
                ? $"{foundNamespace}.Tests"
                : "Tests";

            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using NUnit.Framework;");
            sb.AppendLine("using SecurityRule.Infrastructure;");
            sb.AppendLine();
            sb.AppendLine($"namespace {testNamespace};");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>Integration-тесты для <see cref=\"{className}\"/>.</summary>");
            sb.AppendLine($"/// <remarks>Используют InMemory AppDbContext. Сгенерировано AI агентом.</remarks>");
            sb.AppendLine("[TestFixture]");
            sb.AppendLine($"public class {className}Tests");
            sb.AppendLine("{");
            sb.AppendLine("    private AppDbContext _db = null!;");
            sb.AppendLine($"    private {className} _sut = null!;");
            sb.AppendLine();
            sb.AppendLine("    [SetUp]");
            sb.AppendLine("    public void SetUp()");
            sb.AppendLine("    {");
            sb.AppendLine("        var options = new DbContextOptionsBuilder<AppDbContext>()");
            sb.AppendLine("            .UseInMemoryDatabase(Guid.NewGuid().ToString())");
            sb.AppendLine("            .Options;");
            sb.AppendLine("        _db = new AppDbContext(options);");
            sb.AppendLine($"        _sut = new {className}(_db);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [TearDown]");
            sb.AppendLine("    public void TearDown() => _db.Dispose();");

            foreach (var method in publicMethods)
                AppendNUnitTest(sb, method, async: IsAsync(method));

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendNUnitTest(StringBuilder sb, MethodDeclarationSyntax method, bool async)
        {
            var methodName = method.Identifier.Text;
            var parms = method.ParameterList.Parameters;
            var returnType = method.ReturnType.ToString();

            sb.AppendLine();
            sb.AppendLine("    [Test]");

            if (async)
            {
                sb.AppendLine($"    public async Task {methodName}_Returns_Expected_When_ValidInput()");
                sb.AppendLine("    {");
                sb.AppendLine("        // Arrange");
                foreach (var p in parms)
                    sb.AppendLine($"        {p.Type} {p.Identifier.Text} = default!;");
                sb.AppendLine();
                sb.AppendLine("        // Act");
                var isVoidTask = returnType == "Task" || returnType == "ValueTask";
                var awaitPrefix = isVoidTask ? "await " : "var result = await ";
                var argsStr = string.Join(", ", parms.Select(p => p.Identifier.Text));
                sb.AppendLine($"        {awaitPrefix}_sut.{methodName}({argsStr});");
                sb.AppendLine();
                sb.AppendLine("        // Assert");
                sb.AppendLine("        Assert.That(true, Is.True); // TODO: добавь проверки");
            }
            else
            {
                sb.AppendLine($"    public void {methodName}_Returns_Expected_When_ValidInput()");
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
                sb.AppendLine("        Assert.That(true, Is.True); // TODO: добавь проверки");
            }

            sb.AppendLine("    }");
        }

        private static bool IsAsync(MethodDeclarationSyntax m)
        {
            var ret = m.ReturnType.ToString();
            return ret.StartsWith("Task") || ret.StartsWith("ValueTask");
        }
    }

    // ─── generate_feature ─────────────────────────────────────────────────────

    private sealed class GenerateFeatureTool : IAgentTool
    {
        private readonly string _projectPath;

        public GenerateFeatureTool(string projectPath) => _projectPath = projectPath;

        public string Name => "generate_feature";

        public string Description =>
            "Генерирует заготовку SpecFlow .feature файла (Gherkin) для E2E тестирования Blazor UI. " +
            "Сценарии пишутся на русском языке по соглашению проекта (Given/When/Then на русском). " +
            "Требует наличия Blazor UI страницы с data-testid атрибутами.";

        public object Parameters => new
        {
            type = "object",
            properties = new
            {
                entity_name = new { type = "string", description = "Имя сущности (например: Сервер, Приложение)" },
                entity_name_plural = new { type = "string", description = "Имя сущности во множественном числе (например: Серверы, Приложения)" },
                route = new { type = "string", description = "Маршрут страницы (например: /servers)" },
                output_path = new
                {
                    type = "string",
                    description = "Путь для сохранения .feature файла (относительно проекта). Если не указан — выводит в консоль."
                }
            },
            required = new[] { "entity_name", "entity_name_plural", "route" }
        };

        public async Task<string> ExecuteAsync(JsonElement arguments)
        {
            if (!arguments.TryGetProperty("entity_name", out var nameEl))
                return "Ошибка: не передан параметр entity_name";
            if (!arguments.TryGetProperty("entity_name_plural", out var namePluralEl))
                return "Ошибка: не передан параметр entity_name_plural";
            if (!arguments.TryGetProperty("route", out var routeEl))
                return "Ошибка: не передан параметр route";

            var entityName = nameEl.GetString()?.Trim() ?? "";
            var entityNamePlural = namePluralEl.GetString()?.Trim() ?? "";
            var route = routeEl.GetString()?.Trim() ?? "";

            string? outputPath = null;
            if (arguments.TryGetProperty("output_path", out var outEl))
                outputPath = outEl.GetString()?.Trim().Trim('"', '\'');

            var featureContent = GenerateFeatureContent(entityName, entityNamePlural, route);

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

                await File.WriteAllTextAsync(fullPath, featureContent, Encoding.UTF8);
                return $"✅ Feature файл создан: {Path.GetRelativePath(_projectPath, fullPath)}";
            }

            return $"Сгенерированный .feature файл для {entityNamePlural}:\n\n```gherkin\n{featureContent}\n```";
        }

        private static string GenerateFeatureContent(string name, string namePlural, string route)
        {
            var createRoute = $"{route.TrimEnd('/')}/create";
            var sb = new StringBuilder();
            sb.AppendLine($"# language: ru");
            sb.AppendLine($"Функция: {namePlural}");
            sb.AppendLine($"  Как пользователь системы");
            sb.AppendLine($"  Я хочу управлять {namePlural.ToLowerInvariant()}");
            sb.AppendLine($"  Чтобы поддерживать актуальные данные в системе");
            sb.AppendLine();
            sb.AppendLine($"  Сценарий: Страница {namePlural.ToLowerInvariant()} открывается");
            sb.AppendLine($"    Когда пользователь открывает страницу \"{route}\"");
            sb.AppendLine($"    Тогда заголовок страницы содержит \"{namePlural}\"");
            sb.AppendLine();
            sb.AppendLine($"  Сценарий: Создание нового {name.ToLowerInvariant()}");
            sb.AppendLine($"    Дано пользователь находится на странице \"{route}\"");
            sb.AppendLine($"    Когда пользователь нажимает кнопку добавить");
            sb.AppendLine($"    Тогда открывается страница \"{createRoute}\"");
            sb.AppendLine($"    Когда пользователь заполняет форму и нажимает Сохранить");
            sb.AppendLine($"    Тогда {name.ToLowerInvariant()} появляется в списке");
            sb.AppendLine();
            sb.AppendLine($"  Сценарий: {name} отображается в списке");
            sb.AppendLine($"    Дано в системе есть {name.ToLowerInvariant()} \"Тестовый\"");
            sb.AppendLine($"    Когда пользователь открывает страницу \"{route}\"");
            sb.AppendLine($"    Тогда в таблице отображается {name.ToLowerInvariant()} \"Тестовый\"");
            sb.AppendLine();
            sb.AppendLine($"  Сценарий: Удаление {name.ToLowerInvariant()}");
            sb.AppendLine($"    Дано в системе есть {name.ToLowerInvariant()} \"ДляУдаления\"");
            sb.AppendLine($"    Когда пользователь открывает редактирование {name.ToLowerInvariant()} \"ДляУдаления\"");
            sb.AppendLine($"    И нажимает кнопку Удалить");
            sb.AppendLine($"    Тогда {name.ToLowerInvariant()} \"ДляУдаления\" исчезает из списка");
            return sb.ToString().TrimEnd();
        }
    }

    // ─── Вспомогательные методы ───────────────────────────────────────────────

    private static string DetectLayer(string filePath)
    {
        if (filePath.Contains($"{Path.DirectorySeparatorChar}Domain{Path.DirectorySeparatorChar}") ||
            filePath.Contains("/Domain/"))
            return "domain";
        if (filePath.Contains($"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}") ||
            filePath.Contains("/Infrastructure/"))
            return "infrastructure";
        if (filePath.Contains(".E2E.") || filePath.Contains(".Tests"))
            return "tests";
        if (filePath.Contains($"{Path.DirectorySeparatorChar}Web{Path.DirectorySeparatorChar}") ||
            filePath.Contains("/Web/") || filePath.EndsWith(".razor"))
            return "web";
        return "default";
    }
}

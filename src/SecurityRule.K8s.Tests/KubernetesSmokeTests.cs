using System.Diagnostics;
using System.Net;
using FluentAssertions;

namespace SecurityRule.K8s.Tests;

/// <summary>
/// Smoke-тест инфраструктуры.
///
/// Сценарий:
///   1. Собирает Docker-образ приложения.
///   2. Поднимает локальный kind-кластер с SQL Server и приложением через Terragrunt.
///   3. Загружает образ в kind (imagePullPolicy: Never).
///   4. Ждёт готовности Deployment.
///   5. Отправляет один GET-запрос на корневой endpoint и проверяет HTTP 200.
///   6. Сносит кластер через terragrunt destroy.
///
/// Требования на машине:
///   - Docker
///   - kind   (https://kind.sigs.k8s.io/docs/user/quick-start/#installation)
///   - terraform >= 1.6  (https://developer.hashicorp.com/terraform/install)
///   - terragrunt        (https://terragrunt.gruntwork.io/docs/getting-started/install/)
///   - kubectl
///
/// Тест помечен [Explicit] — не запускается в обычном CI.
/// Ручной запуск:
///   dotnet test src/SecurityRule.K8s.Tests/ --filter "Category=K8s"
/// </summary>
[TestFixture]
[Explicit("Требует Docker, kind, terraform, terragrunt. Запускается вручную или через k8s-smoke.yml.")]
[Category("K8s")]
public class KubernetesSmokeTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string TerragruntDir =
        Path.Combine(RepoRoot, "terragrunt", "environments", "local", "k8s-local");

    private const string AppUrl        = "http://localhost:8080";
    private const string ClusterName   = "security-rule-local";
    private const string ImageTag      = "security-rule:local";
    private const string Namespace     = "security-rule";
    private const string DeploymentName = "security-rule";

    // -------------------------------------------------------------------------
    // Setup: поднимаем кластер и ждём готовности
    // -------------------------------------------------------------------------

    [OneTimeSetUp]
    public async Task DeployToKubernetes()
    {
        // 1. Собираем образ
        await RunAsync("docker", $"build -t {ImageTag} .", RepoRoot);

        // 2. Инициализируем Terragrunt (скачивает провайдеры, настраивает backend)
        await RunAsync("terragrunt", "init", TerragruntDir);

        // 3. Применяем конфигурацию: создаёт kind-кластер, namespace, SQL Server, Deployment
        await RunAsync("terragrunt", "apply -auto-approve", TerragruntDir);

        // 4. Загружаем локальный образ в kind (imagePullPolicy: Never требует этого)
        await RunAsync("kind", $"load docker-image {ImageTag} --name {ClusterName}", RepoRoot);

        // 5. Перезапускаем Deployment, чтобы поды подхватили свежезагруженный образ
        //    (без этого поды, упавшие с ErrImageNeverPull, не перезапустятся автоматически)
        await RunAsync(
            "kubectl",
            $"rollout restart deployment/{DeploymentName} -n {Namespace}",
            RepoRoot);

        // 6. Ждём, пока Deployment станет Available (таймаут 5 минут)
        await RunAsync(
            "kubectl",
            $"wait --for=condition=available deployment/{DeploymentName} -n {Namespace} --timeout=300s",
            RepoRoot);
    }

    // -------------------------------------------------------------------------
    // Тест: единственный HTTP-запрос
    // -------------------------------------------------------------------------

    [Test]
    public async Task Application_Returns_200_On_Root_Endpoint()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var response = await client.GetAsync(AppUrl);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"приложение по адресу {AppUrl} должно отвечать HTTP 200");
    }

    // -------------------------------------------------------------------------
    // TearDown: сносим кластер в любом случае
    // -------------------------------------------------------------------------

    [OneTimeTearDown]
    public async Task DestroyKubernetes()
    {
        try
        {
            await RunAsync("terragrunt", "destroy -auto-approve", TerragruntDir);
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не бросаем исключение —
            // TearDown не должен скрывать результат самого теста
            TestContext.Out.WriteLine($"[TearDown] Ошибка при terragrunt destroy: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Вспомогательные методы
    // -------------------------------------------------------------------------

    /// <summary>
    /// Запускает внешнюю команду и ждёт её завершения.
    /// Выводит stdout/stderr в TestContext. Бросает исключение при ненулевом exit code.
    /// </summary>
    private static async Task RunAsync(string fileName, string arguments, string workingDirectory)
    {
        TestContext.Out.WriteLine($"> {fileName} {arguments}  (cwd: {workingDirectory})");

        var psi = new ProcessStartInfo
        {
            FileName               = fileName,
            Arguments              = arguments,
            WorkingDirectory       = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Не удалось запустить процесс: {fileName}");

        // Читаем stdout и stderr параллельно, чтобы не заблокировать процесс на полном буфере
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stdout)) TestContext.Out.WriteLine(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) TestContext.Out.WriteLine(stderr);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Команда '{fileName} {arguments}' завершилась с кодом {process.ExitCode}.\n{stderr}");
        }
    }

    /// <summary>
    /// Находит корень репозитория, поднимаясь по дереву директорий
    /// до папки, содержащей файл Dockerfile.
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Dockerfile")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Не удалось найти корень репозитория (директорию с файлом Dockerfile). " +
            "Убедитесь, что тест запускается из поддиректории репозитория.");
    }
}

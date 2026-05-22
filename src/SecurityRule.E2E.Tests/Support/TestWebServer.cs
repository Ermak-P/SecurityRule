using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Services;
using SecurityRule.Web;
using SecurityRule.Web.Components;
using SecurityRule.Web.Services;

namespace SecurityRule.E2E.Tests.Support;

/// <summary>
/// Starts a real ASP.NET Core / Kestrel server on a random port using an
/// in-memory EF Core database so that Playwright can connect to it via HTTP.
/// The server reuses the same service registrations as the production app
/// (Program.cs), but replaces SQL Server with EF InMemory.
/// </summary>
public sealed class TestWebServer : IAsyncDisposable
{
    private readonly string _dbName;
    private WebApplication? _app;

    /// <summary>
    /// Creates a new test web server.
    /// Each instance should use a unique <paramref name="dbName"/> so that parallel
    /// feature executions do not share the same in-memory EF Core database.
    /// </summary>
    public TestWebServer(string? dbName = null)
    {
        _dbName = dbName ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>Base URL of the server, e.g. "http://127.0.0.1:54321".</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>Root service provider – use CreateScope() to resolve scoped services.</summary>
    public IServiceProvider Services =>
        _app?.Services ?? throw new InvalidOperationException("Server has not been started.");

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
            // Use the SecurityRule.Web application name so that UseStaticWebAssets()
            // picks up SecurityRule.Web.staticwebassets.runtime.json (which contains
            // _framework/blazor.web.js and all other static web assets).
            ApplicationName = "SecurityRule.Web",
            // Point content root at the SecurityRule.Web project directory so that
            // static web assets (wwwroot, _framework/*.js, _content/* package assets)
            // are found via the Development-mode static web asset file providers.
            ContentRootPath = FindWebProjectDirectory()
        });

        // ── Services ─────────────────────────────────────────────────────────
        builder.Services.AddRazorComponents()
                        .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        builder.Services.AddScoped<AuditSaveChangesInterceptor>();

        // Replace SQL Server with EF InMemory for test isolation.
        // Each TestWebServer instance uses a unique database name to support parallel test execution.
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseInMemoryDatabase(_dbName)
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        // FakeAd also uses an in-memory database for test isolation
        builder.Services.AddDbContextFactory<FakeAdDbContext>(options =>
            options.UseInMemoryDatabase(_dbName + "_FakeAd"));

        builder.Services
            .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddApplicationServices(useActiveDirectory: false);

        // Register FakePartnerService so tests can pre-populate partner data.
        builder.Services.AddSingleton<FakePartnerService>();
        builder.Services.AddSingleton<IPartnerService>(sp => sp.GetRequiredService<FakePartnerService>());

        // Listen on a random free port; no HTTPS required for tests
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // ── Build and configure pipeline ──────────────────────────────────────
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var fakeAdFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FakeAdDbContext>>();
            await using var fakeAdDb = await fakeAdFactory.CreateDbContextAsync();
            await fakeAdDb.Database.EnsureCreatedAsync();
        }

        // In Development mode, UseStaticFiles() automatically includes the static web
        // asset file providers (UseStaticWebAssets), which serve _framework/blazor.web.js
        // and _content/* package assets from embedded assembly resources.
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        await app.StartAsync();

        // Resolve the actual bound address
        var server   = app.Services.GetRequiredService<IServer>();
        var feature  = server.Features.Get<IServerAddressesFeature>();
        BaseUrl      = (feature?.Addresses.FirstOrDefault() ?? "http://127.0.0.1:5000")
                       .TrimEnd('/');

        _app = app;
    }

    /// <summary>
    /// Removes all domain data between scenarios to guarantee test isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Load with related entities so EF tracks the junction-table rows
        var services = await db.AppServices.Include(s => s.Servers).Include(s => s.Tags).Include(s => s.Partners).ToListAsync();
        db.AppServices.RemoveRange(services);
        await db.SaveChangesAsync();

        var servers = await db.Servers.Include(s => s.Tags).ToListAsync();
        db.Servers.RemoveRange(servers);
        db.Tags.RemoveRange(db.Tags);
        db.PartnerNames.RemoveRange(db.PartnerNames);
        db.Certificates.RemoveRange(db.Certificates);
        db.ServiceConnections.RemoveRange(db.ServiceConnections);
        db.Groups.RemoveRange(db.Groups);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        // Reset fake AD state between scenarios
        if (Services.GetRequiredService<IAdService>() is FakeAdService fakeAd)
            fakeAd.Reset();

        // Reset fake partner service state between scenarios
        Services.GetRequiredService<FakePartnerService>().Reset();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FindWebProjectDirectory()
    {
        // Walk up from the test output directory until we find SecurityRule.Web
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            // Typically the solution layout is src/<projects>
            var candidate = Path.Combine(dir.FullName, "SecurityRule.Web");
            if (IsWebProject(candidate)) return candidate;

            candidate = Path.Combine(dir.FullName, "src", "SecurityRule.Web");
            if (IsWebProject(candidate)) return candidate;

            dir = dir.Parent;
        }

        // Fall back to the current directory; static assets may be missing but
        // the app will still render HTML for testing purposes.
        return AppContext.BaseDirectory;
    }

    private static bool IsWebProject(string path) =>
        Directory.Exists(path) &&
        File.Exists(Path.Combine(path, "SecurityRule.Web.csproj"));
}

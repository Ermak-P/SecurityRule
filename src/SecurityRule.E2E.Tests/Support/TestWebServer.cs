using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;
using SecurityRule.Infrastructure.Services;
using SecurityRule.Web.Components;

namespace SecurityRule.E2E.Tests.Support;

/// <summary>
/// Starts a real ASP.NET Core / Kestrel server on a random port using an
/// in-memory EF Core database so that Playwright can connect to it via HTTP.
/// The server reuses the same service registrations as the production app
/// (Program.cs), but replaces SQL Server with EF InMemory.
/// </summary>
public sealed class TestWebServer : IAsyncDisposable
{
    private WebApplication? _app;

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

        // Replace SQL Server with EF InMemory for test isolation
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("E2ETestDb"));

        // FakeAd also uses an in-memory database for test isolation
        builder.Services.AddDbContextFactory<FakeAdDbContext>(options =>
            options.UseInMemoryDatabase("E2EFakeAdDb"));

        builder.Services.AddScoped<IServerRepository, ServerRepository>();
        builder.Services.AddScoped<IAppServiceRepository, AppServiceRepository>();
        builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
        builder.Services.AddScoped<IFirewallRuleRepository, FirewallRuleRepository>();
        builder.Services.AddScoped<SecurityRule.Domain.Interfaces.IOperatingSystemRepository, SecurityRule.Infrastructure.Repositories.OperatingSystemRepository>();
        builder.Services.AddScoped<SecurityRule.Domain.Interfaces.IUserRepository, SecurityRule.Infrastructure.Repositories.UserRepository>();
        builder.Services.AddScoped<SecurityRule.Domain.Interfaces.IGroupRepository, SecurityRule.Infrastructure.Repositories.GroupRepository>();
        builder.Services.AddScoped<SecurityRule.Domain.Interfaces.ISearchService, SecurityRule.Infrastructure.Repositories.SearchService>();
        builder.Services.AddSingleton<SecurityRule.Domain.Interfaces.IAdService, FakeAdService>();
        builder.Services.AddScoped<SecurityRule.Web.Services.ThemeState>();

        // Listen on a random free port; no HTTPS required for tests
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // ── Build and configure pipeline ──────────────────────────────────────
        var app = builder.Build();

        // In Development mode, UseStaticFiles() automatically includes the static web
        // asset file providers (UseStaticWebAssets), which serve _framework/blazor.web.js
        // and _content/* package assets from embedded assembly resources.
        app.UseStaticFiles();
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
        var services = await db.AppServices.Include(s => s.Servers).ToListAsync();
        db.AppServices.RemoveRange(services);
        await db.SaveChangesAsync();

        db.Servers.RemoveRange(db.Servers);
        db.Certificates.RemoveRange(db.Certificates);
        db.FirewallRules.RemoveRange(db.FirewallRules);
        db.Groups.RemoveRange(db.Groups);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        // Reset fake AD state between scenarios
        if (Services.GetRequiredService<IAdService>() is FakeAdService fakeAd)
            fakeAd.Reset();
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

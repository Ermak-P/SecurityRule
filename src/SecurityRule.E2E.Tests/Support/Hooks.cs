using Microsoft.Playwright;
using Reqnroll;

namespace SecurityRule.E2E.Tests.Support;

/// <summary>
/// Reqnroll lifecycle hooks.
/// • [BeforeTestRun]  – start the Blazor test server and a headless Chromium instance once.
/// • [AfterTestRun]   – dispose browser and server.
/// • [BeforeScenario] – reset the database and open a fresh browser page.
/// • [AfterScenario]  – close the browser context used by the scenario.
/// </summary>
[Binding]
public sealed class Hooks
{
    // ── Static resources shared across all scenarios ──────────────────────────
    private static TestWebServer? _server;
    private static IPlaywright?   _playwright;
    private static IBrowser?      _browser;

    private readonly ScenarioState _state;

    public Hooks(ScenarioState state) => _state = state;

    // ── One-time setup / teardown ─────────────────────────────────────────────

    [BeforeTestRun]
    public static async Task BeforeTestRunAsync()
    {
        _server = new TestWebServer();
        await _server.StartAsync();

        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_server is not null) await _server.DisposeAsync();
    }

    // ── Per-scenario setup / teardown ─────────────────────────────────────────

    [BeforeScenario]
    public async Task BeforeScenarioAsync()
    {
        // Start each scenario with a clean in-memory database
        await _server!.ResetDatabaseAsync();

        // Create an isolated browser context (own cookies / storage) per scenario
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            // Ignore SSL errors if any (not expected since we use HTTP)
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        _state.Page     = page;
        _state.BaseUrl  = _server.BaseUrl;
        _state.Services = _server.Services;
    }

    [AfterScenario]
    public async Task AfterScenarioAsync()
    {
        if (_state.Page is not null)
            await _state.Page.Context.DisposeAsync();
    }
}

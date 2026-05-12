using Microsoft.Playwright;
using Reqnroll;

namespace SecurityRule.E2E.Tests.Support;

/// <summary>
/// Reqnroll lifecycle hooks.
/// <para>
/// Parallelism model: NUnit runs each feature (fixture) in parallel via
/// <c>[assembly: Parallelizable(ParallelScope.Fixtures)]</c>.  Each feature gets
/// its own <see cref="TestWebServer"/> (unique in-memory database), its own
/// Playwright instance and browser, stored in the <see cref="FeatureContext"/>.
/// Scenarios within a feature still execute sequentially and share one server,
/// resetting the database between them.
/// </para>
/// • [BeforeFeature]  – start the Blazor test server and a headless Chromium instance.
/// • [AfterFeature]   – dispose browser and server.
/// • [BeforeScenario] – reset the database and open a fresh browser page.
/// • [AfterScenario]  – close the browser context used by the scenario.
/// </summary>
[Binding]
public sealed class Hooks
{
    private readonly ScenarioState _state;
    private readonly FeatureContext _featureContext;

    public Hooks(ScenarioState state, FeatureContext featureContext)
    {
        _state = state;
        _featureContext = featureContext;
    }

    // ── Per-feature setup / teardown ──────────────────────────────────────────

    [BeforeFeature]
    public static async Task BeforeFeatureAsync(FeatureContext featureContext)
    {
        var server = new TestWebServer();
        await server.StartAsync();

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        featureContext.Set(server);
        featureContext.Set(playwright);
        featureContext.Set(browser);
    }

    [AfterFeature]
    public static async Task AfterFeatureAsync(FeatureContext featureContext)
    {
        if (featureContext.TryGetValue<IBrowser>(out var browser))
            await browser.DisposeAsync();

        if (featureContext.TryGetValue<IPlaywright>(out var playwright))
            playwright.Dispose();

        if (featureContext.TryGetValue<TestWebServer>(out var server))
            await server.DisposeAsync();
    }

    // ── Per-scenario setup / teardown ─────────────────────────────────────────

    [BeforeScenario]
    public async Task BeforeScenarioAsync()
    {
        if (!_featureContext.TryGetValue<TestWebServer>(out var server))
            throw new InvalidOperationException(
                "TestWebServer not found in FeatureContext. Did BeforeFeatureAsync fail?");

        if (!_featureContext.TryGetValue<IBrowser>(out var browser))
            throw new InvalidOperationException(
                "IBrowser not found in FeatureContext. Did BeforeFeatureAsync fail?");

        // Start each scenario with a clean in-memory database
        await server.ResetDatabaseAsync();

        // Create an isolated browser context (own cookies / storage) per scenario
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        _state.Page     = page;
        _state.BaseUrl  = server.BaseUrl;
        _state.Services = server.Services;
    }

    [AfterScenario]
    public async Task AfterScenarioAsync()
    {
        if (_state.Page is not null)
            await _state.Page.Context.DisposeAsync();
    }
}

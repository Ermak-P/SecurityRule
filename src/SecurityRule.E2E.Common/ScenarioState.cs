using Microsoft.Playwright;

namespace SecurityRule.E2E.Common;

/// <summary>
/// Per-scenario shared state injected into all step-definition classes by Reqnroll.
/// </summary>
public class ScenarioState
{
    /// <summary>Active Playwright page for the current scenario.</summary>
    public IPage Page { get; set; } = null!;

    /// <summary>Base URL of the running test web server (e.g. http://127.0.0.1:5xxx).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Root DI container of the test web server, used to seed/reset data.</summary>
    public IServiceProvider Services { get; set; } = null!;
}

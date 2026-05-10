using Microsoft.Playwright;

namespace SecurityRule.E2E.Tests.Support;

/// <summary>
/// Shared Playwright actions for Blazor Server UI used across step definitions.
/// </summary>
public static class BlazorUiActions
{
    public static async Task NavigateAndWaitForBlazorAsync(this IPage page, string url, int postRenderDelayMs = 500)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        await page.WaitForTimeoutAsync(postRenderDelayMs);
    }

    public static async Task ClickButtonAndWaitAsync(this IPage page, string buttonText, int postClickDelayMs = 300)
    {
        await page
            .GetByRole(AriaRole.Button, new() { Name = buttonText, Exact = true })
            .ClickAsync();
        await page.WaitForTimeoutAsync(postClickDelayMs);
    }
}

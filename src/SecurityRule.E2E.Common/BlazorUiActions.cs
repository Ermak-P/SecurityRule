using Microsoft.Playwright;

namespace SecurityRule.E2E.Common;

/// <summary>
/// Shared Playwright actions for Blazor Server UI used across step definitions.
/// </summary>
public static class BlazorUiActions
{
    /// <summary>
    /// Navigates to the given URL, waits for the Blazor Server SignalR circuit to connect
    /// (i.e. <c>window.Blazor._internal.navigationManager</c> is available), then pauses for
    /// <paramref name="postRenderDelayMs"/> milliseconds to allow interactive components to
    /// finish their first render cycle before assertions begin.
    /// </summary>
    /// <param name="page">The Playwright page to navigate.</param>
    /// <param name="url">The full URL to navigate to.</param>
    /// <param name="postRenderDelayMs">
    /// Additional delay in milliseconds after the Blazor runtime is ready. Increase for pages
    /// that perform async data loading after the circuit is established (default: 500 ms).
    /// </param>
    public static async Task NavigateAndWaitForBlazorAsync(this IPage page, string url, int postRenderDelayMs = 500)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        await page.WaitForTimeoutAsync(postRenderDelayMs);
    }

    /// <summary>
    /// Clicks a button identified by its ARIA role and exact visible text, then pauses for
    /// <paramref name="postClickDelayMs"/> milliseconds to allow the Blazor Server SignalR
    /// round-trip to complete (e.g. save + navigate) before the next step runs.
    /// </summary>
    /// <param name="page">The Playwright page containing the button.</param>
    /// <param name="buttonText">The exact visible text of the button to click.</param>
    /// <param name="postClickDelayMs">
    /// Delay in milliseconds after the click. The SignalR WebSocket stays open so
    /// NetworkIdle is not used; this small delay lets the server handler execute (default: 300 ms).
    /// </param>
    public static async Task ClickButtonAndWaitAsync(this IPage page, string buttonText, int postClickDelayMs = 300)
    {
        await page
            .GetByRole(AriaRole.Button, new() { Name = buttonText, Exact = true })
            .ClickAsync();
        await page.WaitForTimeoutAsync(postClickDelayMs);
    }
}

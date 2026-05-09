using Microsoft.Playwright;

namespace SecurityRule.E2E.Tests.Support;

public static class PlaywrightWaits
{
    public static async Task WaitForBlazorReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null,
            new() { Timeout = 15_000, PollingInterval = 200 });

        var appContent = page.Locator(".mud-main-content, main, h1, h2").First;
        await appContent.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    }
}

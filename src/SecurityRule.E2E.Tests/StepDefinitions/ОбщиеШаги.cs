using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions shared between server and service scenarios:
/// general navigation, form interactions, and assertions.
/// </summary>
[Binding]
public sealed class ОбщиеШаги
{
    private readonly ScenarioState _state;

    public ОбщиеШаги(ScenarioState state) => _state = state;

    // ── Form interactions ─────────────────────────────────────────────────────

    /// <summary>
    /// Fills a MudTextField or MudAutocomplete by its visible label text.
    /// MudBlazor renders &lt;label for="id"&gt; + &lt;input id="id"&gt;, so
    /// Playwright's GetByLabel resolves it correctly.
    /// For MudAutocomplete (CoerceText=true) pressing Tab after fill coerces the
    /// typed value and closes any open dropdown.
    /// </summary>
    [When("я заполняю поле {string} значением {string}")]
    public async Task ЗаполнитьПоле(string label, string value)
    {
        var input = _state.Page.GetByLabel(label, new() { Exact = true });
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    /// <summary>Clears the current value then types the new one (used in edit scenarios).</summary>
    [When("я заменяю значение поля {string} на {string}")]
    public async Task ЗаменитьЗначениеПоля(string label, string newValue)
    {
        var input = _state.Page.GetByLabel(label, new() { Exact = true });
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await input.FillAsync(string.Empty);
        await input.FillAsync(newValue);
    }

    /// <summary>Clicks a MudButton by its visible text label.</summary>
    [When("я нажимаю кнопку {string}")]
    public async Task НажатьКнопку(string buttonText)
    {
        await _state.Page
            .GetByRole(AriaRole.Button, new() { Name = buttonText, Exact = true })
            .ClickAsync();
        // Allow Blazor's SignalR round-trip to deliver the click event to the server
        // and give the server handler time to execute (save + navigate or similar).
        // We do NOT use NetworkIdle here because the SignalR WebSocket stays open.
        await _state.Page.WaitForTimeoutAsync(300);
    }

    /// <summary>Selects an option by text in a MudSelect dropdown by its label.</summary>
    [When("я выбираю {string} в выпадающем списке {string}")]
    public async Task ВыбратьВВыпадающемСписке(string value, string label)
    {
        // Find the MudSelect container by its visible label text, then click the input root to open
        var container = _state.Page.Locator(".mud-input-control")
            .Filter(new LocatorFilterOptions { HasText = label });
        await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await container.Locator(".mud-input-root").First.ClickAsync();
        // Wait for the popover list to appear and click the matching item
        await _state.Page.WaitForTimeoutAsync(400);
        var option = _state.Page.GetByRole(AriaRole.Option, new() { Name = value });
        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await option.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(200);
    }

    // ── Assertions ────────────────────────────────────────────────────────────

    /// <summary>Asserts that a heading (h1–h4) with the given text is visible.</summary>
    [Then("я вижу заголовок {string}")]
    public async Task ВидетьЗаголовок(string heading)
    {
        // Wait for Blazor interactive rendering to produce the heading
        await Assertions
            .Expect(_state.Page.Locator($"h1,h2,h3,h4,h5").Filter(new() { HasText = heading }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    /// <summary>Asserts that the specified text is visible anywhere on the page.</summary>
    [Then("я вижу текст {string} на странице")]
    public async Task ВидетьТекст(string text)
    {
        // Use .First to avoid strict-mode violations when the text appears in
        // multiple elements (e.g. both a breadcrumb link and a heading).
        await Assertions
            .Expect(_state.Page.GetByText(text, new() { Exact = false }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    /// <summary>Asserts that the specified text does NOT appear on the page.</summary>
    [Then("на странице нет текста {string}")]
    public async Task НетТекста(string text)
    {
        // After deletion the page navigates to the list; wait for the list to render
        // then confirm the deleted item is absent.
        await Assertions
            .Expect(_state.Page.GetByText(text, new() { Exact = false }))
            .ToBeHiddenAsync(new() { Timeout = 15_000 });
    }

    /// <summary>Asserts that the current URL contains the given path segment.</summary>
    [Then("URL страницы содержит {string}")]
    public async Task URLСодержит(string path)
    {
        await Assertions
            .Expect(_state.Page)
            .ToHaveURLAsync(new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(path)),
                new() { Timeout = 15_000 });
    }

    /// <summary>Asserts that the current URL ends with the given path.</summary>
    [Then("я нахожусь на странице {string}")]
    public async Task НаходитьсяНаСтранице(string path)
    {
        await Assertions
            .Expect(_state.Page)
            .ToHaveURLAsync(new Regex(Regex.Escape(path) + @"(\?.*)?$"),
                new() { Timeout = 15_000 });
    }
}

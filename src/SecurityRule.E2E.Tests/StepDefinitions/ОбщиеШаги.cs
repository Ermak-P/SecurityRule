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
        // Find the MudSelect container by its visible label text.
        var container = _state.Page.Locator(".mud-input-control")
            .Filter(new LocatorFilterOptions { HasText = label });
        await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

        // MudBlazor 9 opens the dropdown via @onmousedown="HandleMouseDown" on MudInputControl
        // (exposed as a typed OnMouseDown EventCallback parameter). ClickAsync fires mousedown
        // which triggers HandleMouseDown → ToggleMenu → OpenMenu. The subsequent click event
        // has no handler that closes the menu, so the popover stays open.
        await container.First.ClickAsync();

        // Wait for the popover list to appear.
        // MudBlazor 9 renders select items as div.mud-list-item (no role="option").
        await _state.Page.WaitForTimeoutAsync(500);

        var option = _state.Page.Locator(".mud-list-item")
            .Filter(new LocatorFilterOptions { HasText = value });
        await option.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });
        await option.First.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(200);
    }

    // ── Assertions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that an input field (identified by its visible label) has the given value.
    /// Used to verify that clone forms are pre-filled with source entity data.
    /// Note: works for standard MudTextField fields; complex autocomplete fields may need
    /// alternative locator strategies.
    /// </summary>
    [Then("поле {string} содержит значение {string}")]
    public async Task ПолеСодержитЗначение(string label, string expectedValue)
    {
        var input = _state.Page.GetByLabel(label, new() { Exact = true });
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await Assertions.Expect(input).ToHaveValueAsync(expectedValue, new() { Timeout = 10_000 });
    }

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

    /// <summary>Opens a MudSelect dropdown and asserts that an option with the given text is visible.</summary>
    [Then("я вижу текст {string} в выпадающем списке {string}")]
    public async Task ВидетьТекстВВыпадающемСписке(string value, string label)
    {
        var container = _state.Page.Locator(".mud-input-control")
            .Filter(new LocatorFilterOptions { HasText = label });
        await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

        // ClickAsync fires mousedown → HandleMouseDown → ToggleMenu → OpenMenu (MudBlazor 9).
        await container.First.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(500);

        // MudBlazor 9 renders select items as div.mud-list-item (no role="option").
        var option = _state.Page.Locator(".mud-list-item")
            .Filter(new LocatorFilterOptions { HasText = value });

        await Assertions
            .Expect(option.First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await _state.Page.Keyboard.PressAsync("Escape");
        await _state.Page.WaitForTimeoutAsync(200);
    }

    /// <summary>Opens a MudSelect dropdown and asserts that an option with the given text is NOT present.</summary>
    [Then("в выпадающем списке {string} нет текста {string}")]
    public async Task НетТекстаВВыпадающемСписке(string label, string value)
    {
        var container = _state.Page.Locator(".mud-input-control")
            .Filter(new LocatorFilterOptions { HasText = label });
        await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        var inputEl2 = container.Locator("input.mud-select-input").First;
        await inputEl2.FocusAsync();
        await _state.Page.WaitForTimeoutAsync(500);

        // MudBlazor 9 renders select items as div.mud-list-item (no role="option").
        // When the dropdown is closed the popover content is not in the DOM at all,
        // so ToBeHiddenAsync passes immediately; the FocusAsync above is just a
        // precaution to trigger any pending render.
        var option = _state.Page.Locator(".mud-list-item")
            .Filter(new LocatorFilterOptions { HasText = value });

        await Assertions
            .Expect(option.First)
            .ToBeHiddenAsync(new() { Timeout = 5_000 });

        await _state.Page.Keyboard.PressAsync("Escape");
        await _state.Page.WaitForTimeoutAsync(200);
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

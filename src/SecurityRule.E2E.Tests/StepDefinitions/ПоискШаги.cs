using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Поиск" (Full-text search) feature.
/// </summary>
[Binding]
public sealed class ПоискШаги
{
    private readonly ScenarioState _state;

    public ПоискШаги(ScenarioState state) => _state = state;

    // ── Given: seed helpers reused by search scenarios ────────────────────────

    /// <summary>Seeds a service with a specific UserName for AD-account search tests.</summary>
    [Given("в системе существует сервис Name {string} UserName {string}")]
    public async Task ВСистемеСуществуетСервисСUserName(string name, string userName)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        await repo.AddAsync(new AppService { Name = name, UserName = userName });
    }

    /// <summary>Seeds a user with given Name and Description for search tests.</summary>
    [Given("в системе существует пользователь Name {string} Description {string}")]
    public async Task ВСистемеСуществуетПользовательNameDescription(string name, string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IUserRepository>();
        await repo.AddAsync(new User { Name = name, Description = description });
    }

    /// <summary>Seeds a group with given Name and Description for search tests.</summary>
    [Given("в системе существует группа Name {string} Description {string}")]
    public async Task ВСистемеСуществуетГруппаNameDescription(string name, string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IGroupRepository>();
        await repo.AddAsync(new Group { Name = name, Description = description });
    }

    // ── When: search interactions ─────────────────────────────────────────────

    /// <summary>Types text into the global search field in the AppBar and waits for results.</summary>
    [When("я ввожу в поле поиска текст {string}")]
    public async Task ВвестиВПолеПоискаТекст(string text)
    {
        // The search input has placeholder "Поиск по всем записям..."
        var input = _state.Page.GetByPlaceholder("Поиск по всем записям...");
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await input.FillAsync(text);
        // Wait for debounce (300ms) + server search + render
        await _state.Page.WaitForTimeoutAsync(800);
    }

    [When("я нажимаю на первый результат поиска")]
    public async Task НажатьНаПервыйРезультатПоиска()
    {
        // Results are rendered as MudListItem children inside the search-results panel
        var resultsPanel = _state.Page.Locator("[data-testid='search-results']");
        await resultsPanel.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10_000 });
        var firstResult = resultsPanel.Locator(".mud-list-item").First;
        await firstResult.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        await firstResult.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(500);
    }

    // ── Then: search assertions ───────────────────────────────────────────────

    [Then("я вижу поле поиска в шапке")]
    public async Task ВидетьПолеПоискаВШапке()
    {
        var input = _state.Page.GetByPlaceholder("Поиск по всем записям...");
        await Assertions.Expect(input).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then("в результатах поиска я вижу {string}")]
    public async Task ВРезультатахПоискаЯВижу(string text)
    {
        if (text == "Ничего не найдено")
        {
            // Check the no-results panel
            var noResultsPanel = _state.Page.Locator("[data-testid='search-no-results']");
            await Assertions.Expect(noResultsPanel).ToBeVisibleAsync(new() { Timeout = 10_000 });
            return;
        }
        // Check the search-results panel contains the text
        var resultsPanel = _state.Page.Locator("[data-testid='search-results']");
        await resultsPanel.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10_000 });
        await Assertions.Expect(resultsPanel.GetByText(text, new() { Exact = false }).First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then("в результатах поиска найденный текст {string} выделен")]
    public async Task НайденныйТекстВыделен(string query)
    {
        // The highlight is rendered as a <mark> element containing the query text
        var resultsPanel = _state.Page.Locator("[data-testid='search-results']");
        await resultsPanel.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10_000 });
        var mark = resultsPanel.Locator("mark").Filter(new() { HasText = query });
        await Assertions.Expect(mark.First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then("выпадающий список поиска не отображается")]
    public async Task ВыпадающийСписокНеОтображается()
    {
        // When query length < 2, no dropdown should appear
        await _state.Page.WaitForTimeoutAsync(600);
        await Assertions.Expect(_state.Page.Locator("[data-testid='search-results']")).ToHaveCountAsync(0, new() { Timeout = 5_000 });
        await Assertions.Expect(_state.Page.Locator("[data-testid='search-no-results']")).ToHaveCountAsync(0, new() { Timeout = 5_000 });
    }
}

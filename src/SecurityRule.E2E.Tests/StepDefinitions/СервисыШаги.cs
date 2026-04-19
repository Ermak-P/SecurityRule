using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Сервисы" (Services) feature.
/// </summary>
[Binding]
public sealed class СервисыШаги
{
    private readonly ScenarioState _state;

    public СервисыШаги(ScenarioState state) => _state = state;

    // ── Given: seed data directly into the in-memory database ────────────────

    /// <summary>Creates a service (no linked servers) directly in the database.</summary>
    [Given("в системе существует сервис Name {string} AD {string}")]
    public async Task ВСистемеСуществуетСервис(string name, string ad)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        await repo.AddAsync(new AppService { Name = name, UserName = ad });
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу сервисов")]
    public async Task ПерейтиНаСтраницуСервисов()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/services");
    }

    [When("я перехожу на страницу добавления сервиса")]
    public async Task ПерейтиНаСтраницуДобавленияСервиса()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/services/create");
    }

    [When("я открываю страницу деталей сервиса {string}")]
    public async Task ОткрытьСтраницуДеталейСервиса(string name)
    {
        var id = await GetServiceIdAsync(name);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/services/{id}");
    }

    [When("я открываю страницу редактирования сервиса {string}")]
    public async Task ОткрытьСтраницуРедактированияСервиса(string name)
    {
        var id = await GetServiceIdAsync(name);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/services/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetServiceIdAsync(string name)
    {
        using var scope = _state.Services.CreateScope();
        var repo     = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        var services = await repo.GetAllAsync();
        return services.First(s => s.Name == name).Id;
    }

    private async Task NavigateAndWaitAsync(string url)
    {
        // WaitUntil=Load ensures blazor.web.js is downloaded and executed.
        await _state.Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        // Wait for Blazor Server's circuit to connect and make components interactive.
        // After blazor.web.js initialises and the SignalR circuit is established,
        // window.Blazor._internal.navigationManager becomes available.
        await _state.Page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        // Brief grace period for the interactive component tree to finish rendering
        await _state.Page.WaitForTimeoutAsync(500);
    }
}

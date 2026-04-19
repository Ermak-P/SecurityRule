using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Серверы" (Servers) feature.
/// </summary>
[Binding]
public sealed class СерверыШаги
{
    private readonly ScenarioState _state;

    public СерверыШаги(ScenarioState state) => _state = state;

    // ── Given: seed data directly into the in-memory database ────────────────

    /// <summary>
    /// Creates a server with the given Name / IP / OS directly in the database
    /// (bypassing the UI) so that other steps can test read/edit/delete flows.
    /// </summary>
    [Given("в системе существует сервер Name {string} IP {string} OS {string}")]
    public async Task ВСистемеСуществуетСервер(string name, string ip, string os)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        await repo.AddAsync(new Server { Name = name, IpAddress = ip, OperatingSystem = os });
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу серверов")]
    public async Task ПерейтиНаСтраницуСерверов()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/servers");
    }

    [When("я перехожу на страницу добавления сервера")]
    public async Task ПерейтиНаСтраницуДобавленияСервера()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/servers/create");
    }

    [When("я открываю страницу деталей сервера {string}")]
    public async Task ОткрытьСтраницуДеталейСервера(string name)
    {
        var id = await GetServerIdAsync(name);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/servers/{id}");
    }

    [When("я открываю страницу редактирования сервера {string}")]
    public async Task ОткрытьСтраницуРедактированияСервера(string name)
    {
        var id = await GetServerIdAsync(name);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/servers/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetServerIdAsync(string name)
    {
        using var scope = _state.Services.CreateScope();
        var repo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var servers = await repo.GetAllAsync();
        return servers.First(s => s.Name == name).Id;
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

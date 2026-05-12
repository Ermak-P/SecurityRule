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
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/servers");
    }

    [When("я перехожу на страницу добавления сервера")]
    public async Task ПерейтиНаСтраницуДобавленияСервера()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/servers/create");
    }

    [When("я открываю страницу деталей сервера {string}")]
    public async Task ОткрытьСтраницуДеталейСервера(string name)
    {
        var id = await GetServerIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/servers/{id}");
    }

    [When("я открываю страницу редактирования сервера {string}")]
    public async Task ОткрытьСтраницуРедактированияСервера(string name)
    {
        var id = await GetServerIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/servers/edit/{id}");
    }

    [When("я открываю страницу клонирования сервера {string}")]
    public async Task ОткрытьСтраницуКлонированияСервера(string name)
    {
        var id = await GetServerIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/servers/create?cloneFrom={id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetServerIdAsync(string name)
    {
        using var scope = _state.Services.CreateScope();
        var repo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var servers = await repo.GetAllAsync();
        return servers.First(s => s.Name == name).Id;
    }
}

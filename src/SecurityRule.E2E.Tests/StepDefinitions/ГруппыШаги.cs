using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Группы" (Groups) feature.
/// </summary>
[Binding]
public sealed class ГруппыШаги
{
    private readonly ScenarioState _state;

    public ГруппыШаги(ScenarioState state) => _state = state;

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу групп")]
    public async Task ПерейтиНаСтраницуГрупп()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/groups");
    }

    [When("я перехожу на страницу добавления группы")]
    public async Task ПерейтиНаСтраницуДобавленияГруппы()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/groups/create");
    }

    [When("я открываю страницу деталей группы {string}")]
    public async Task ОткрытьСтраницуДеталейГруппы(string name)
    {
        var id = await GetGroupIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/groups/{id}");
    }

    [When("я открываю страницу редактирования группы {string}")]
    public async Task ОткрытьСтраницуРедактированияГруппы(string name)
    {
        var id = await GetGroupIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/groups/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetGroupIdAsync(string name)
    {
        using var scope = _state.Services.CreateScope();
        var repo   = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IGroupRepository>();
        var groups = await repo.GetAllAsync();
        return groups.First(g => g.Name == name).Id;
    }
}

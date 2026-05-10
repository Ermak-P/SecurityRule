using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Common;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Пользователи" (Users) feature.
/// </summary>
[Binding]
public sealed class ПользователиШаги
{
    private readonly ScenarioState _state;

    public ПользователиШаги(ScenarioState state) => _state = state;

    // ── Given: seed data ─────────────────────────────────────────────────────

    [Given("в системе существует пользователь с именем {string} и описанием {string}")]
    public async Task ВСистемеСуществуетПользователь(string name, string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IUserRepository>();
        await repo.AddAsync(new User { Name = name, Description = description });
    }

    [Given("в системе существует группа с именем {string} и описанием {string}")]
    public async Task ВСистемеСуществуетГруппа(string name, string description)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IGroupRepository>();
        await repo.AddAsync(new Group { Name = name, Description = description });
    }

    [Given("пользователь {string} входит в группу {string}")]
    public void ПользовательВходитВГруппу(string userName, string groupName)
    {
        var fakeAd = _state.Services.GetRequiredService<SecurityRule.Domain.Interfaces.IAdService>()
                     as SecurityRule.Infrastructure.Services.FakeAdService;
        fakeAd?.AddUserToGroup(userName, groupName);
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу пользователей")]
    public async Task ПерейтиНаСтраницуПользователей()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/users");
    }

    [When("я перехожу на страницу добавления пользователя")]
    public async Task ПерейтиНаСтраницуДобавленияПользователя()
    {
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/users/create");
    }

    [When("я открываю страницу деталей пользователя {string}")]
    public async Task ОткрытьСтраницуДеталейПользователя(string name)
    {
        var id = await GetUserIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/users/{id}");
    }

    [When("я открываю страницу редактирования пользователя {string}")]
    public async Task ОткрытьСтраницуРедактированияПользователя(string name)
    {
        var id = await GetUserIdAsync(name);
        await _state.Page.NavigateAndWaitForBlazorAsync($"{_state.BaseUrl}/users/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetUserIdAsync(string name)
    {
        using var scope = _state.Services.CreateScope();
        var repo  = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IUserRepository>();
        var users = await repo.GetAllAsync();
        return users.First(u => u.Name == name).Id;
    }
}

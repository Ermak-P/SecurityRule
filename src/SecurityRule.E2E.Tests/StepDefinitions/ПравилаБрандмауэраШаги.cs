using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions specific to the "Правила брандмауэра" (Firewall Rules) feature.
/// </summary>
[Binding]
public sealed class ПравилаБрандмауэраШаги
{
    private readonly ScenarioState _state;

    public ПравилаБрандмауэраШаги(ScenarioState state) => _state = state;

    // ── Given: seed data directly into the in-memory database ────────────────

    /// <summary>Creates a firewall rule directly in the database.</summary>
    [Given("в системе существует правило фаервола SourceIp {string} DestIp {string}")]
    public async Task ВСистемеСуществуетПравилоФаервола(string sourceIp, string destIp)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();
        await repo.AddAsync(new FirewallRule
        {
            SourceIp = sourceIp,
            DestinationIp = destIp,
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = $"{sourceIp} -> {destIp}"
        });
    }

    /// <summary>Creates a firewall rule linked to a server (by name) directly in the database.</summary>
    [Given("в системе существует правило фаервола для сервера {string} с описанием {string}")]
    public async Task ВСистемеСуществуетПравилоФаерволаДляСервера(string serverName, string description)
    {
        using var scope = _state.Services.CreateScope();
        var serverRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var ruleRepo   = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();

        var servers = await serverRepo.GetAllAsync();
        var server  = servers.First(s => s.Name == serverName);

        await ruleRepo.AddAsync(new FirewallRule
        {
            ServerId    = server.Id,
            ExpiresAt   = DateTime.Now.AddYears(1),
            Description = description
        });
    }

    /// <summary>Creates a firewall rule linked to a service (by name) directly in the database.</summary>
    [Given("в системе существует правило фаервола для сервиса {string} с описанием {string}")]
    public async Task ВСистемеСуществуетПравилоФаерволаДляСервиса(string serviceName, string description)
    {
        using var scope = _state.Services.CreateScope();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        var ruleRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();

        var services = await serviceRepo.GetAllAsync();
        var service  = services.First(s => s.Name == serviceName);

        await ruleRepo.AddAsync(new FirewallRule
        {
            ServiceId   = service.Id,
            ExpiresAt   = DateTime.Now.AddYears(1),
            Description = description
        });
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу правил фаервола")]
    public async Task ПерейтиНаСтраницуПравилФаервола()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules");
    }

    [When("я перехожу на страницу добавления правила фаервола")]
    public async Task ПерейтиНаСтраницуДобавленияПравилаФаервола()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules/create");
    }

    [When("я открываю страницу деталей правила фаервола SourceIp {string} DestIp {string}")]
    public async Task ОткрытьСтраницуДеталейПравилаФаервола(string sourceIp, string destIp)
    {
        var id = await GetFirewallRuleIdAsync(sourceIp, destIp);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules/{id}");
    }

    [When("я открываю страницу редактирования правила фаервола SourceIp {string} DestIp {string}")]
    public async Task ОткрытьСтраницуРедактированияПравилаФаервола(string sourceIp, string destIp)
    {
        var id = await GetFirewallRuleIdAsync(sourceIp, destIp);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetFirewallRuleIdAsync(string sourceIp, string destIp)
    {
        using var scope = _state.Services.CreateScope();
        var repo  = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();
        var rules = await repo.GetAllAsync();
        return rules.First(r => r.SourceIp == sourceIp && r.DestinationIp == destIp).Id;
    }

    private async Task NavigateAndWaitAsync(string url)
    {
        await _state.Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await _state.Page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        await _state.Page.WaitForTimeoutAsync(500);
    }
}

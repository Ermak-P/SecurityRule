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

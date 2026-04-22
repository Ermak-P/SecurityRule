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

    /// <summary>Creates a service directly in the database.</summary>
    [Given("в системе существует сервис Name {string} UserName {string}")]
    public async Task ВСистемеСуществуетСервис(string name, string userName)
    {
        using var scope = _state.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        await repo.AddAsync(new AppService { Name = name, UserName = userName });
    }

    /// <summary>Creates a firewall rule linked to 4 entities by name.</summary>
    [Given("в системе существует правило фаервола от сервера {string} сервиса {string} до сервера {string} сервиса {string}")]
    public async Task ВСистемеСуществуетПравилоФаервола(
        string srcServerName, string srcServiceName, string dstServerName, string dstServiceName)
    {
        using var scope = _state.Services.CreateScope();
        var serverRepo  = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();
        var ruleRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();

        var servers  = (await serverRepo.GetAllAsync()).ToList();
        var services = (await serviceRepo.GetAllAsync()).ToList();

        var srcSrv = servers.First(s => s.Name == srcServerName);
        var dstSrv = servers.First(s => s.Name == dstServerName);
        var srcSvc = services.First(s => s.Name == srcServiceName);
        var dstSvc = services.First(s => s.Name == dstServiceName);

        await ruleRepo.AddAsync(new FirewallRule
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServerId  = dstSrv.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol  = "TCP",
            Action    = "Allow",
            Direction = "Inbound",
            Description = $"{srcServerName}/{srcServiceName} -> {dstServerName}/{dstServiceName}"
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

    [When("я открываю страницу деталей правила фаервола от {string} до {string}")]
    public async Task ОткрытьСтраницуДеталейПравилаФаервола(string srcServerName, string dstServerName)
    {
        var id = await GetFirewallRuleIdAsync(srcServerName, dstServerName);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules/{id}");
    }

    [When("я открываю страницу редактирования правила фаервола от {string} до {string}")]
    public async Task ОткрытьСтраницуРедактированияПравилаФаервола(string srcServerName, string dstServerName)
    {
        var id = await GetFirewallRuleIdAsync(srcServerName, dstServerName);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/firewall-rules/edit/{id}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetFirewallRuleIdAsync(string srcServerName, string dstServerName)
    {
        using var scope = _state.Services.CreateScope();
        var ruleRepo   = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IFirewallRuleRepository>();
        var serverRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();

        var servers = (await serverRepo.GetAllAsync()).ToList();
        var srcSrv  = servers.First(s => s.Name == srcServerName);
        var dstSrv  = servers.First(s => s.Name == dstServerName);

        var rules = await ruleRepo.GetAllAsync();
        return rules.First(r => r.SourceServerId == srcSrv.Id && r.DestinationServerId == dstSrv.Id).Id;
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


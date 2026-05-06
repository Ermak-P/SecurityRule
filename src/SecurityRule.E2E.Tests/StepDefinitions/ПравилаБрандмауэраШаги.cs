using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Reqnroll;
using SecurityRule.Domain.Models;
using SecurityRule.E2E.Tests.Support;

namespace SecurityRule.E2E.Tests.StepDefinitions;

/// <summary>
/// Step definitions for the "Связи" (ServiceConnections) feature.
/// </summary>
[Binding]
public sealed class ПравилаБрандмауэраШаги
{
    private readonly ScenarioState _state;

    public ПравилаБрандмауэраШаги(ScenarioState state) => _state = state;

    // ── Given: seed data directly into the in-memory database ────────────────

    /// <summary>Links an existing service to an existing server (many-to-many).</summary>
    [Given("сервис {string} прикреплён к серверу {string}")]
    public async Task СервисПрикреплёнКСерверу(string serviceName, string serverName)
    {
        using var scope = _state.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityRule.Infrastructure.Data.AppDbContext>();

        var server  = db.Servers.Include(s => s.Services).First(s => s.Name == serverName);
        var service = db.AppServices.First(s => s.Name == serviceName);
        if (!server.Services.Any(s => s.Id == service.Id))
        {
            server.Services.Add(service);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Creates a service connection with source server + service and destination service.</summary>
    [Given("в системе существует связь от сервера {string} сервиса {string} до сервиса {string}")]
    public async Task ВСистемеСуществуетСвязьОтСервераСервисаДоСервиса(
        string srcServerName, string srcServiceName, string dstServiceName)
    {
        using var scope = _state.Services.CreateScope();
        var connectionRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServiceConnectionRepository>();
        var serverRepo     = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var serviceRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();

        var servers  = (await serverRepo.GetAllAsync()).ToList();
        var services = (await serviceRepo.GetAllAsync()).ToList();

        var srcSrv = servers.First(s => s.Name == srcServerName);
        var srcSvc = services.First(s => s.Name == srcServiceName);
        var dstSvc = services.First(s => s.Name == dstServiceName);

        await connectionRepo.AddAsync(new ServiceConnection
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol = "TCP"
        });
    }

    /// <summary>Creates a service connection with no source server (source service + destination service only).</summary>
    [Given("в системе существует связь без сервера источника от сервиса {string} до сервиса {string}")]
    public async Task ВСистемеСуществуетСвязьБезСервераИсточника(string srcServiceName, string dstServiceName)
    {
        using var scope = _state.Services.CreateScope();
        var connectionRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServiceConnectionRepository>();
        var serviceRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();

        var services = (await serviceRepo.GetAllAsync()).ToList();
        var srcSvc = services.First(s => s.Name == srcServiceName);
        var dstSvc = services.First(s => s.Name == dstServiceName);

        await connectionRepo.AddAsync(new ServiceConnection
        {
            SourceServiceId      = srcSvc.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol = "TCP"
        });
    }

    // ── When: navigation ──────────────────────────────────────────────────────

    [When("я перехожу на страницу связей")]
    public async Task ПерейтиНаСтраницуСвязей()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections");
    }

    [When("я перехожу на страницу карты связей")]
    public async Task ПерейтиНаСтраницуКартыСвязей()
    {
        var url = $"{_state.BaseUrl}/connections/map";
        await _state.Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await _state.Page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        // Wait for Cytoscape canvas – graphMap.init() is called from OnAfterRenderAsync
        // via JS interop, which only runs after Blazor becomes interactive (not in SSR
        // prerender). This guarantees the component is fully rendered and data is loaded.
        await _state.Page.WaitForSelectorAsync("#cy-container canvas",
            new() { Timeout = 15_000 });
        await _state.Page.WaitForTimeoutAsync(300);
    }

    [When("я перехожу на страницу добавления связи")]
    public async Task ПерейтиНаСтраницуДобавленияСвязи()
    {
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections/create");
    }

    [When("я открываю страницу деталей связи с источником {string} и назначением {string}")]
    public async Task ОткрытьСтраницуДеталейСвязи(string srcName, string dstServiceName)
    {
        var id = await GetConnectionIdAsync(srcName, dstServiceName);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections/{id}");
    }

    [When("я открываю страницу редактирования связи с источником {string} и назначением {string}")]
    public async Task ОткрытьСтраницуРедактированияСвязи(string srcName, string dstServiceName)
    {
        var id = await GetConnectionIdAsync(srcName, dstServiceName);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections/edit/{id}");
    }

    [When("я открываю страницу клонирования связи с источником {string} и назначением {string}")]
    public async Task ОткрытьСтраницуКлонированияСвязи(string srcName, string dstServiceName)
    {
        var id = await GetConnectionIdAsync(srcName, dstServiceName);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections/create?cloneFrom={id}");
    }

    [When("я открываю страницу редактирования связи без сервера источника с назначением {string}")]
    public async Task ОткрытьСтраницуРедактированияСвязиБезСервера(string dstServiceName)
    {
        using var scope = _state.Services.CreateScope();
        var connectionRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServiceConnectionRepository>();
        var serviceRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();

        var services = (await serviceRepo.GetAllAsync()).ToList();
        var dstSvc = services.First(s => s.Name == dstServiceName);
        var connections = await connectionRepo.GetAllAsync();
        var connection = connections.First(c => c.DestinationServiceId == dstSvc.Id && c.SourceServerId == null);
        await NavigateAndWaitAsync($"{_state.BaseUrl}/connections/edit/{connection.Id}");
    }

    [Then("граф карты связей содержит canvas элемент")]
    public async Task ГрафСодержитCanvasЭлемент()
    {
        var canvas = _state.Page.Locator("[data-testid='connections-graph'] canvas").First;
        await canvas.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15_000 });
    }

    /// <summary>
    /// Verifies that a server chip is currently selected (has mud-chip-selected class).
    /// </summary>
    [Then("чип сервера {string} выбран")]
    public async Task ЧипСервераВыбран(string label)
    {
        var chip = _state.Page
            .Locator("[data-testid='server-filter-chips'] .mud-chip-selected")
            .Filter(new LocatorFilterOptions { HasText = label });
        await Assertions.Expect(chip.First).ToBeAttachedAsync(new() { Timeout = 10_000 });
    }

    /// <summary>
    /// Verifies that a server chip is NOT selected (does not have mud-chip-selected class).
    /// </summary>
    [Then("чип сервера {string} не выбран")]
    public async Task ЧипСервераНеВыбран(string label)
    {
        var chip = _state.Page
            .Locator("[data-testid='server-filter-chips'] .mud-chip-selected")
            .Filter(new LocatorFilterOptions { HasText = label });
        await Assertions.Expect(chip).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    /// <summary>
    /// Clicks a server chip to toggle its selection state.
    /// </summary>
    [When("я снимаю выделение с чипа сервера {string}")]
    public async Task СнятьВыделениеСЧипаСервера(string label)
    {
        var chip = _state.Page
            .Locator("[data-testid='server-filter-chips'] .mud-chip")
            .Filter(new LocatorFilterOptions { HasText = label });
        await chip.First.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(600);
    }

    /// <summary>
    /// Verifies that a server appears in the hidden server-filter-options list
    /// rendered by GraphMap.razor. This avoids opening the MudSelect dropdown,
    /// which is unreliable after SSR-prerender → InteractiveServer adoption.
    /// </summary>
    [Then("в фильтре серверов доступен сервер {string}")]
    public async Task ВФильтреСервероВДоступенСервер(string serverLabel)
    {
        var option = _state.Page
            .Locator("[data-testid='server-filter-options'] span")
            .Filter(new LocatorFilterOptions { HasText = serverLabel });
        await Assertions
            .Expect(option.First)
            .ToBeAttachedAsync(new() { Timeout = 10_000 });
    }

    /// <summary>
    /// Clicks the "Показывать связанные серверы" MudCheckBox to toggle it on/off.
    /// MudBlazor 9.3 MudCheckBox renders a .mud-checkbox-input span with an @onclick handler
    /// inside the .mud-checkbox root span.
    /// </summary>
    [When("я включаю галочку {string}")]
    public async Task ВключитьГалочку(string labelText)
    {
        var wrapper = _state.Page.Locator("[data-testid='show-related-checkbox']");
        await wrapper.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        // Try clicking the mud-checkbox-input first; fall back to the root element
        var inputEl = wrapper.Locator(".mud-checkbox-input");
        if (await inputEl.CountAsync() > 0)
            await inputEl.First.ClickAsync();
        else
            await wrapper.First.ClickAsync();
        await _state.Page.WaitForTimeoutAsync(600);
    }

    /// <summary>
    /// Asserts that a server appears in the hidden graph-dimmed-servers list,
    /// meaning it is currently shown as a related (dimmed) server.
    /// </summary>
    [Then("граф содержит связанный сервер {string}")]
    public async Task ГрафСодержитСвязанныйСервер(string serverLabel)
    {
        var span = _state.Page
            .Locator("[data-testid='graph-dimmed-servers'] span")
            .Filter(new LocatorFilterOptions { HasText = serverLabel });
        await Assertions.Expect(span.First).ToBeAttachedAsync(new() { Timeout = 10_000 });
    }

    /// <summary>
    /// Asserts that a server does NOT appear in the hidden graph-dimmed-servers list.
    /// </summary>
    [Then("граф не содержит связанный сервер {string}")]
    public async Task ГрафНеСодержитСвязанныйСервер(string serverLabel)
    {
        var span = _state.Page
            .Locator("[data-testid='graph-dimmed-servers'] span")
            .Filter(new LocatorFilterOptions { HasText = serverLabel });
        await Assertions.Expect(span).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetConnectionIdAsync(string srcName, string dstServiceName)
    {
        using var scope = _state.Services.CreateScope();
        var connectionRepo = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServiceConnectionRepository>();
        var serverRepo     = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IServerRepository>();
        var serviceRepo    = scope.ServiceProvider.GetRequiredService<SecurityRule.Domain.Interfaces.IAppServiceRepository>();

        var servers  = (await serverRepo.GetAllAsync()).ToList();
        var services = (await serviceRepo.GetAllAsync()).ToList();

        var dstSvc = services.First(s => s.Name == dstServiceName);
        var connections = await connectionRepo.GetAllAsync();

        // Try match by source server name first, then by source service name
        var srcServer = servers.FirstOrDefault(s => s.Name == srcName);
        if (srcServer != null)
            return connections.First(c => c.SourceServerId == srcServer.Id && c.DestinationServiceId == dstSvc.Id).Id;

        var srcService = services.FirstOrDefault(s => s.Name == srcName);
        if (srcService != null)
            return connections.First(c => c.SourceServiceId == srcService.Id && c.DestinationServiceId == dstSvc.Id).Id;

        return connections.First(c => c.DestinationServiceId == dstSvc.Id).Id;
    }

    private async Task NavigateAndWaitAsync(string url)
    {
        await _state.Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        await _state.Page.WaitForFunctionAsync(
            "() => window.Blazor && window.Blazor._internal && !!window.Blazor._internal.navigationManager",
            null, new() { Timeout = 15_000, PollingInterval = 200 });
        await _state.Page.WaitForTimeoutAsync(1500);
    }
}


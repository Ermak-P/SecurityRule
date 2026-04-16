using FluentAssertions;
using Reqnroll;
using SecurityRule.BDD.Tests.Support;
using SecurityRule.Domain.Models;

namespace SecurityRule.BDD.Tests.StepDefinitions;

[Binding]
public class ServiceStepDefinitions
{
    private readonly ScenarioState _state;
    private AppService? _foundService;

    public ServiceStepDefinitions(ScenarioState state)
    {
        _state = state;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the service database is empty")]
    public void GivenTheServiceDatabaseIsEmpty() { }

    [Given("a service {string} with AD account {string} exists")]
    public async Task GivenAServiceExists(string name, string adAccount)
    {
        var lastServer = _state.LastServerId > 0
            ? await _state.ServerRepository.GetByIdAsync(_state.LastServerId)
            : null;
        var servers = lastServer != null ? new List<Server> { lastServer } : new List<Server>();
        var service = new AppService { Name = name, AdAccountName = adAccount, Servers = servers };
        await _state.AppServiceRepository.AddAsync(service);
        _state.LastServiceId = service.Id;
    }

    [Given("the following services exist:")]
    public async Task GivenTheFollowingServicesExist(DataTable dataTable)
    {
        var lastServer = _state.LastServerId > 0
            ? await _state.ServerRepository.GetByIdAsync(_state.LastServerId)
            : null;
        var servers = lastServer != null ? new List<Server> { lastServer } : new List<Server>();
        foreach (var row in dataTable.Rows)
        {
            var service = new AppService
            {
                Name = row["Name"],
                AdAccountName = row["AdAccountName"],
                Servers = servers
            };
            await _state.AppServiceRepository.AddAsync(service);
        }
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When("I add a service with name {string} and AD account {string}")]
    public async Task WhenIAddAService(string name, string adAccount)
    {
        var lastServer = _state.LastServerId > 0
            ? await _state.ServerRepository.GetByIdAsync(_state.LastServerId)
            : null;
        var servers = lastServer != null ? new List<Server> { lastServer } : new List<Server>();
        var service = new AppService { Name = name, AdAccountName = adAccount, Servers = servers };
        await _state.AppServiceRepository.AddAsync(service);
        _state.LastServiceId = service.Id;
    }

    [When("I add a service {string} with AD account {string} linked to both servers")]
    public async Task WhenIAddAServiceLinkedToBothServers(string name, string adAccount)
    {
        var allServers = (await _state.ServerRepository.GetAllAsync()).ToList();
        var service = new AppService { Name = name, AdAccountName = adAccount, Servers = allServers };
        await _state.AppServiceRepository.AddAsync(service);
        _state.LastServiceId = service.Id;
        _foundService = await _state.AppServiceRepository.GetByIdAsync(service.Id);
    }

    [When("I request all services")]
    public Task WhenIRequestAllServices() => Task.CompletedTask;

    [When("I search for the service by its ID")]
    public async Task WhenISearchForTheServiceByItsId()
    {
        _foundService = await _state.AppServiceRepository.GetByIdAsync(_state.LastServiceId);
    }

    [When("I search for the service with ID {int}")]
    public async Task WhenISearchForTheServiceWithId(int id)
    {
        _foundService = await _state.AppServiceRepository.GetByIdAsync(id);
    }

    [When("I update the service name to {string}")]
    public async Task WhenIUpdateTheServiceNameTo(string newName)
    {
        var service = await _state.AppServiceRepository.GetByIdAsync(_state.LastServiceId);
        service.Should().NotBeNull();
        service!.Name = newName;
        await _state.AppServiceRepository.UpdateAsync(service);
        _foundService = await _state.AppServiceRepository.GetByIdAsync(_state.LastServiceId);
    }

    [When("I delete the service")]
    public async Task WhenIDeleteTheService()
    {
        await _state.AppServiceRepository.DeleteAsync(_state.LastServiceId);
    }

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then("the service list should contain {int} service(s)")]
    public async Task ThenTheServiceListShouldContain(int count)
    {
        var services = await _state.AppServiceRepository.GetAllAsync();
        services.Should().HaveCount(count);
    }

    [Then("the service {string} should exist in the list")]
    public async Task ThenTheServiceShouldExistInTheList(string name)
    {
        var services = await _state.AppServiceRepository.GetAllAsync();
        services.Should().Contain(s => s.Name == name);
    }

    [Then("the service should be found")]
    public void ThenTheServiceShouldBeFound()
    {
        _foundService.Should().NotBeNull();
    }

    [Then("no service should be found")]
    public void ThenNoServiceShouldBeFound()
    {
        _foundService.Should().BeNull();
    }

    [Then("the service name should be {string}")]
    public void ThenTheServiceNameShouldBe(string name)
    {
        _foundService!.Name.Should().Be(name);
    }

    [Then("the service should have the name {string}")]
    public void ThenTheServiceShouldHaveTheName(string name)
    {
        _foundService!.Name.Should().Be(name);
    }

    [Then("the service list should be empty")]
    public async Task ThenTheServiceListShouldBeEmpty()
    {
        var services = await _state.AppServiceRepository.GetAllAsync();
        services.Should().BeEmpty();
    }

    [Then("the service {string} should be linked to {int} servers")]
    public void ThenTheServiceShouldBeLinkedToServers(string _, int count)
    {
        _foundService.Should().NotBeNull();
        _foundService!.Servers.Should().HaveCount(count);
    }

    [Then("the service should include {int} server(s)")]
    public void ThenTheServiceShouldIncludeServers(int count)
    {
        _foundService.Should().NotBeNull();
        _foundService!.Servers.Should().HaveCount(count);
    }
}

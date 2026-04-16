using FluentAssertions;
using Reqnroll;
using SecurityRule.BDD.Tests.Support;
using SecurityRule.Domain.Models;

namespace SecurityRule.BDD.Tests.StepDefinitions;

[Binding]
public class ServerStepDefinitions
{
    private readonly ScenarioState _state;
    private Server? _foundServer;

    public ServerStepDefinitions(ScenarioState state)
    {
        _state = state;
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the server database is empty")]
    public void GivenTheServerDatabaseIsEmpty() { }

    [Given("a service {string} with AD account {string} is linked to the server")]
    public async Task GivenAServiceIsLinkedToTheServer(string serviceName, string adAccount)
    {
        var server = await _state.ServerRepository.GetByIdAsync(_state.LastServerId);
        server.Should().NotBeNull();
        var service = new AppService
        {
            Name = serviceName,
            AdAccountName = adAccount,
            Servers = [server!]
        };
        await _state.AppServiceRepository.AddAsync(service);
    }

    // ─── When ────────────────────────────────────────────────────────────────

    [When("I add a server with name {string}, IP {string} and OS {string}")]
    public async Task WhenIAddAServer(string name, string ip, string os)
    {
        var server = new Server { Name = name, IpAddress = ip, OperatingSystem = os };
        await _state.ServerRepository.AddAsync(server);
        _state.LastServerId = server.Id;
    }

    [When("I request all servers")]
    public Task WhenIRequestAllServers() => Task.CompletedTask;

    [When("I search for the server by its ID")]
    public async Task WhenISearchForTheServerByItsId()
    {
        _foundServer = await _state.ServerRepository.GetByIdAsync(_state.LastServerId);
    }

    [When("I search for the server with ID {int}")]
    public async Task WhenISearchForTheServerWithId(int id)
    {
        _foundServer = await _state.ServerRepository.GetByIdAsync(id);
    }

    [When("I update the server name to {string}")]
    public async Task WhenIUpdateTheServerNameTo(string newName)
    {
        var server = await _state.ServerRepository.GetByIdAsync(_state.LastServerId);
        server.Should().NotBeNull();
        server!.Name = newName;
        await _state.ServerRepository.UpdateAsync(server);
        _foundServer = await _state.ServerRepository.GetByIdAsync(_state.LastServerId);
    }

    [When("I delete the server")]
    public async Task WhenIDeleteTheServer()
    {
        await _state.ServerRepository.DeleteAsync(_state.LastServerId);
    }

    [When("I delete the server with ID {int}")]
    public async Task WhenIDeleteTheServerWithId(int id)
    {
        try
        {
            await _state.ServerRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _state.ThrownException = ex;
        }
    }

    // ─── Then ────────────────────────────────────────────────────────────────

    [Then("the server list should contain {int} server(s)")]
    public async Task ThenTheServerListShouldContain(int count)
    {
        var servers = await _state.ServerRepository.GetAllAsync();
        servers.Should().HaveCount(count);
    }

    [Then("the server {string} should exist in the list")]
    public async Task ThenTheServerShouldExistInTheList(string name)
    {
        var servers = await _state.ServerRepository.GetAllAsync();
        servers.Should().Contain(s => s.Name == name);
    }

    [Then("the server should be found")]
    public void ThenTheServerShouldBeFound()
    {
        _foundServer.Should().NotBeNull();
    }

    [Then("no server should be found")]
    public void ThenNoServerShouldBeFound()
    {
        _foundServer.Should().BeNull();
    }

    [Then("the server name should be {string}")]
    public void ThenTheServerNameShouldBe(string name)
    {
        _foundServer!.Name.Should().Be(name);
    }

    [Then("the server should have the name {string}")]
    public void ThenTheServerShouldHaveTheName(string name)
    {
        _foundServer!.Name.Should().Be(name);
    }

    [Then("the server list should be empty")]
    public async Task ThenTheServerListShouldBeEmpty()
    {
        var servers = await _state.ServerRepository.GetAllAsync();
        servers.Should().BeEmpty();
    }

    [Then("no exception should be thrown")]
    public void ThenNoExceptionShouldBeThrown()
    {
        _state.ThrownException.Should().BeNull();
    }

    [Then("the server should include {int} service(s)")]
    public void ThenTheServerShouldIncludeServices(int count)
    {
        _foundServer.Should().NotBeNull();
        _foundServer!.Services.Should().HaveCount(count);
    }
}

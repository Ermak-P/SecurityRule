using Reqnroll;
using SecurityRule.BDD.Tests.Support;
using SecurityRule.Domain.Models;

namespace SecurityRule.BDD.Tests.StepDefinitions;

/// <summary>
/// Step definitions that are reused across multiple features
/// (e.g. setting up a server before testing services).
/// </summary>
[Binding]
public class SharedStepDefinitions
{
    private readonly ScenarioState _state;

    public SharedStepDefinitions(ScenarioState state)
    {
        _state = state;
    }

    [Given("a server {string} with IP {string} and OS {string} exists")]
    public async Task GivenAServerExists(string name, string ip, string os)
    {
        var server = new Server { Name = name, IpAddress = ip, OperatingSystem = os };
        await _state.ServerRepository.AddAsync(server);
        _state.LastServerId = server.Id;
    }

    [Given("the following servers exist:")]
    public async Task GivenTheFollowingServersExist(DataTable dataTable)
    {
        foreach (var row in dataTable.Rows)
        {
            var server = new Server
            {
                Name = row["Name"],
                IpAddress = row["IpAddress"],
                OperatingSystem = row["OperatingSystem"]
            };
            await _state.ServerRepository.AddAsync(server);
        }
    }
}

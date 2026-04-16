using Reqnroll;
using Reqnroll.BoDi;
using SecurityRule.BDD.Tests.Support;

namespace SecurityRule.BDD.Tests.Support;

[Binding]
public sealed class Hooks
{
    private readonly IObjectContainer _container;

    public Hooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void RegisterScenarioState()
    {
        var state = new ScenarioState();
        _container.RegisterInstanceAs(state);
    }

    [AfterScenario]
    public void DisposeScenarioState()
    {
        if (_container.IsRegistered<ScenarioState>())
            _container.Resolve<ScenarioState>().Dispose();
    }
}

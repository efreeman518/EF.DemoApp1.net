namespace Test.SpecFlow.Hooks;
[Binding]
public sealed class HookInitialization(ScenarioContext scenarioContext)
{
    // For additional details on SpecFlow hooks see http://go.specflow.org/doc-hooks

    private readonly ScenarioContext _scenarioContext = scenarioContext;

    [BeforeScenario("@tag1")]
    public void BeforeScenarioWithTag()
    {
        // Example of filtering hooks using tags. (in this case, this 'before scenario' hook will execute if the feature/scenario contains the tag '@tag1')
        // See https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html?highlight=hooks#tag-scoping

        _scenarioContext.GetHashCode();
        Console.WriteLine("BeforeScenarioWithTag");
    }

    [BeforeScenario(Order = 1)]
    public void FirstBeforeScenario()
    {
        // Example of ordering the execution of hooks
        // See https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html?highlight=order#hook-execution-order

        _scenarioContext.GetHashCode();
        Console.WriteLine("FirstBeforeScenario");
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _scenarioContext.GetHashCode();
        Console.WriteLine("AfterScenario");
    }
}

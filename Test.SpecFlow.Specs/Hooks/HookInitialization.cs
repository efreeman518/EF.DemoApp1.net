using OpenQA.Selenium;

namespace Test.SpecFlow.Specs.Hooks;
[Binding]
public sealed class HookInitialization
{
    // For additional details on SpecFlow hooks see http://go.specflow.org/doc-hooks

    private readonly ScenarioContext _scenarioContext;

    public HookInitialization(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

    [BeforeScenario("@tag1")]
    public void BeforeScenarioWithTag()
    {
        // Example of filtering hooks using tags. (in this case, this 'before scenario' hook will execute if the feature/scenario contains the tag '@tag1')
        // See https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html?highlight=hooks#tag-scoping

        Console.WriteLine("BeforeScenarioWithTag");
    }

    [BeforeScenario(Order = 1)]
    public void FirstBeforeScenario()
    {
        // Example of ordering the execution of hooks
        // See https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html?highlight=order#hook-execution-order

        Console.WriteLine("FirstBeforeScenario");
    }

    [AfterScenario]
    public void AfterScenario()
    {
        Console.WriteLine("AfterScenario");
    }
}

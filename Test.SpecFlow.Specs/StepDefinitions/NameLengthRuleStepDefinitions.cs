using Domain.Model;
using Domain.Rules;

//https://docs.specflow.org/projects/specflow/en/latest/Execution/Parallel-Execution.html
[assembly: DoNotParallelize]

namespace Test.SpecFlow.Specs.StepDefinitions;

[Binding]
public class NameLengthRuleStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public NameLengthRuleStepDefinitions(ScenarioContext scenatioContext)
    {
        _scenarioContext = scenatioContext;
        _scenarioContext["TodoItem"] = new TodoItem($"a{Guid.NewGuid()}") { CreatedBy = "Test.SpecFlow.Specs" };
        _scenarioContext["TodoNameLengthRule"] = new TodoNameLengthRule();
    }

    [Given(@"the Name length requirement is (\d+)")]
    public void GivenTheNameLengthRequirementIs(int len)
    {
        _scenarioContext["TodoNameLengthRule"] = new TodoNameLengthRule(len);
    }

    [Given(@"the TodoItem Name is (.*)")]
    public void GivenTheTodoItemNameIs(string name)
    {
        _scenarioContext["TodoItem"] = new TodoItem(name) { CreatedBy = "Test.SpecFlow.Specs" };
    }

    [When(@"the TodoItem Name is validated")]
    public void WhenTheTodoItemNameIsValidated()
    {
        _scenarioContext["Result"] = ((TodoNameLengthRule)_scenarioContext["TodoNameLengthRule"]).IsSatisfiedBy(((TodoItem)_scenarioContext["TodoItem"]));
    }

    [Then(@"the valid result should be (true|false)")]
    public void ThenTheResultShouldTrue(bool result)
    {
        Assert.AreEqual((bool)_scenarioContext["Result"], result);
    }
}

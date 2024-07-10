using TechTalk.SpecFlow;
using Test.PlaywrightUI.PageObjects;

namespace Test.PlaywrightUI.Steps;

[Binding]
public class TodoItemCrudSteps(ScenarioContext scenarioContext, MainPageObject mainPage)
{
    [Given(@"the user navigates to the main page")]
    public async Task GivenTheClientNavigatesToThePage()
    {
        await mainPage.NavigateAsync();
    }

    [When(@"user enters (.*) in textbox and clicks Add")]
    public async Task WhenTheClientEntersIntoTheNameField(string value)
    {
        scenarioContext.Add("name", $"{value}-{DateTime.UtcNow.Ticks}");

        await mainPage.EnterName(scenarioContext["name"].ToString()!);
        await mainPage.ClickSaveButton();
    }

    [Then(@"verify the item exists in the grid")]
    public async Task VerifyItemExistsInGrid()
    {
        Assert.IsTrue(await mainPage.GridItemExists(scenarioContext["name"].ToString()));
    }

    [When(@"user clicks the edit button for this item")]
    public async Task WhenUserClicksTheEditButtonForThisItem()
    {
        await mainPage.ClickEditButton(scenarioContext["name"].ToString());
    }

    [Then(@"verify the edit area shows the item")]
    public async Task ThenVerifyTheEditAreaShowsTheItem()
    {
        Assert.IsTrue(await mainPage.EditAreaShowsItem(scenarioContext["name"].ToString()));
    }

    [When(@"user appends the name with (.*) and clicks Update")]
    public async Task WhenUserAppendsTheNameWithAndClicksSave(string appendValue)
    {
        scenarioContext["name"] = scenarioContext["name"].ToString() + appendValue;
        await mainPage.EnterName(scenarioContext["name"].ToString()!);
        await mainPage.ClickSaveButton();
    }

    [When(@"user checks the complete box and clicks Update")]
    public async Task WhenUserChecksTheCompleteBoxAndClicksSave()
    {
        await mainPage.ClickCompleteCheckbox();
        await mainPage.ClickSaveButton();
    }

    [Then(@"verify the item complete box is checked in the grid")]
    public async Task ThenVerifyTheItemCompleteBoxIsCheckedInTheGrid()
    {
        Assert.IsTrue(await mainPage.GridRowCompleteBoxCheckedValue(scenarioContext["name"].ToString()));
    }

    [When(@"user clicks the delete button for this item")]
    public async Task WhenUserClicksTheDeleteButtonForThisItem()
    {
        await mainPage.ClickDeleteButton(scenarioContext["name"].ToString());
    }

    [Then(@"verify the item is no longer in the grid")]
    public async Task ThenVerifyTheItemIsNoLongerInTheGrid()
    {
        Assert.IsTrue(await mainPage.GridItemDoesNotExist(scenarioContext["name"].ToString()));
    }
}

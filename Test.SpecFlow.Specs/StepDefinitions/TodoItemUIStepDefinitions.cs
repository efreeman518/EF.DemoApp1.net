using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;

namespace Test.SpecFlow.Specs.StepDefinitions;

/// <summary>
/// https://www.selenium.dev/documentation/webdriver/elements/finders/
/// #Run the api in another VS
/// #The versions of Chrome and Edge must match the versions of the drivers in the bin folder
/// #This scenario does not currently search or page through items, so less than 10 in the DB required to show on the first page
/// </summary>
[Binding]
public class TodoItemUIStepDefinitions : SeleniumTestBase
{
    public TodoItemUIStepDefinitions(ScenarioContext scenarioContext) : base(scenarioContext)
    {
        _scenarioContext["random"] = RandomString(5);
    }

    //[Given(@"name-value pair params")]
    //public void GivenTheClientConfiguration(Table table)
    //{
    //    dynamic data = table.CreateDynamicInstance();
    //    //use table name-value pairs
    //}

    [Given(@"the client configuration (.*)")]

    public void GivenTheClientConfiguration(string browser)
    {
        CreateDriver(browser);
    }

    [Given(@"user browser navigates to (.*)")]
    public void GivenNavigate(string url)
    {
        _driver!.Navigate().GoToUrl(url);
        Assert.IsTrue(_driver.Title.Contains("SampleApp - Todo CRUD"));
    }

    [When(@"enters (.*) in textbox and clicks Add")]
    public void GivenEnterValueInTextbox(string value)
    {
        _scenarioContext["value"] = value + _scenarioContext["random"];
        _waitMax10Seconds!.Until(ExpectedConditions.ElementExists(By.XPath("//input[@id='edit-name']")))
            .SendKeys(_scenarioContext["value"].ToString());
        _driver!.FindElement(By.XPath("//table//button[@id='btn-save']"))
            .Click();
    }

    [Then(@"verify the item exists in the list")]
    public void ThenVerifyTheItemExistsInTheList()
    {
        //TODO - this will fail if the previous step's todo item does not show on the first page here; need to page through until found
        var found = _waitMax10Seconds!.Until(ExpectedConditions.ElementExists(By.XPath("//tbody[@id='todos']/tr/td[contains(text(),'" + _scenarioContext["value"] + "')]")));
        Assert.IsTrue(found != null);
    }

    [When(@"user clicks the edit button for this item")]
    public void WhenUserClicksTheEditButtonForThisItem()
    {
        _driver!.FindElement(By.XPath("//tbody[@id='todos']/tr[td[contains(text(),'" + _scenarioContext["value"] + "')]]//button[text()='Edit']"))
           .Click();
    }

    [Then(@"verify the edit area shows the item")]
    public void ThenVerifyTheEditAreaShowsTheItem()
    {
        var inputEditName = _waitMax10Seconds!.Until(ExpectedConditions.ElementExists(By.XPath("//input[@id='edit-name']")));
        Assert.IsTrue(inputEditName.GetAttribute("value") == _scenarioContext["value"].ToString());
        _scenarioContext["input-edit-name"] = inputEditName;
    }

    [When(@"user appends the name with (.*) and clicks save")]
    public void WhenUserAppendsTheNameAndClicksSave(string appendName)
    {
        _scenarioContext["value"] += appendName;
        ((IWebElement)_scenarioContext["input-edit-name"]).SendKeys(appendName);
        _driver!.FindElement(By.XPath("//table//button[@id='btn-save']"))
           .Click();
    }

    [When(@"user checks the complete box and clicks save")]
    public void WhenUserChecksTheCompleteBoxAndClicksSave()
    {
        _driver!.FindElement(By.Id("edit-isComplete")).Click();
        _driver!.FindElement(By.XPath("//table//button[@id='btn-save']"))
           .Click();
    }

    [Then(@"verify the item complete box is checked in the list")]
    public void ThenVerifyTheItemCompleteBoxIsCheckedInTheList()
    {
        Thread.Sleep(1000); //wait for refresh with same elements, otherwise next line finds the old element which is stale on the following GetAttribute()
        var found = _waitMax10Seconds!.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id='todos']/tr[td[contains(text(),'" + _scenarioContext["value"] + "')]]/td/input[@type='checkbox']")));
        Assert.IsTrue(bool.Parse(found.GetAttribute("checked"))); //OpenQA.Selenium.StaleElementReferenceException: 'stale element reference: element is not attached to the page document'
    }

    [When(@"user clicks the delete button for this item")]
    public void WhenUserClicksTheDeleteButtonForThisItem()
    {
        _driver!.FindElement(By.XPath("//tbody[@id='todos']/tr[td[contains(text(),'" + _scenarioContext["value"] + "')]]//button[text()='Delete']"))
          .Click();
    }

    [Then(@"verify the item is no longer in the list")]
    public void ThenVerifyTheItemIsNoLongerInTheList()
    {
        try
        {
            _ = _driver!.FindElement(By.XPath("//tbody[@id='todos']/tr[td[contains(text(),'" + _scenarioContext["value"] + "')]]"));
        }
        catch (NoSuchElementException)
        {
            //expected
            Assert.IsTrue(true);
        }
    }

}

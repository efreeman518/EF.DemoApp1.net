using BoDi;
using Microsoft.Playwright;
using TechTalk.SpecFlow;
using Test.PlaywrightUI.PageObjects;

namespace Test.PlaywrightUI.Hooks;

/// <summary>
/// Code to run before and after features/scenarios/steps/test runs
/// </summary>
[Binding]
public static class TestHooks
{
    //private readonly ScenarioContext _scenarioContext;
    //private static IPlaywright _playwright;
    //private static IBrowser _browser;
    //private static IPage _page;

    /// <summary>
    /// Each scenario will have a new browser instance
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    [BeforeScenario]
    public static async Task BeforeTodoItemCrud(IObjectContainer container)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false }); //SlowMo = 100 
        var pageObject = new MainPageObject(browser);

        container.RegisterInstanceAs(playwright);
        container.RegisterInstanceAs(browser);
        container.RegisterInstanceAs(pageObject);
    }

    [AfterScenario]
    public static async Task AfterScenario(IObjectContainer container)
    {
        var playwright = container.Resolve<IPlaywright>();
        var browser = container.Resolve<IBrowser>();

        await browser.CloseAsync();
        playwright.Dispose();
    }
}
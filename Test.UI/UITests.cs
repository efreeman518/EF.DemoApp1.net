using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;

namespace Test.UI;

//[Ignore("api needs to be running somewhere")]
[TestClass]
public class UITests : SeleniumTestBase
{
    public UITests()
    {
    }

    [TestMethod]
    public void Todo_CRUD_pass()
    {
        //nav to swagger page
        string path = $"{_config.GetValue<string>("SampeApi:BaseUrl")}";
        _webDriver.Navigate().GoToUrl(path);
        Assert.IsTrue(_webDriver.Title.Contains("SampleApp - Todo CRUD"));

    }

    [TestCleanup]
    public void Cleanup()
    {
        _webDriver.Quit();
    }
}

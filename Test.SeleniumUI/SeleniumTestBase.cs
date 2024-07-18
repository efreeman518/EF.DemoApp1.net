using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Test.SeleniumUI;

/// <summary>
/// Testing http endpoints (MVC controllers, razor pages)
/// </summary>
[TestClass]
public abstract class SeleniumTestBase : IDisposable
{
    public readonly static IConfigurationRoot Config = Config ?? Support.Utility.BuildConfiguration().Build();
    //protected IConfigurationRoot _config;
    protected IWebDriver _webDriver;
    protected WebDriverWait _waitMax10Seconds;

    protected SeleniumTestBase()
    {
        //_config = Support.Utility.BuildConfiguration().Build();

        //https://github.com/rosolko/WebDriverManager.Net
        //new DriverManager().SetUpDriver(new ChromeConfig());

        //chrome driver download: http://chromedriver.storage.googleapis.com/index.html
        //put the chromedriver.exe in the /drivers folder, properties - Copy to Output folder, and note the version here:
        //var chromeVersion = "97.0.4692.71";
        //_webDriver = new ChromeDriver($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\Chrome\\{chromeVersion}\\X64");

        _webDriver = new ChromeDriver();
        _waitMax10Seconds = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
    }

    [TestInitialize]
    public void Init()
    {
        //test initialize
    }


    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _webDriver != null)
        {
            _webDriver.Quit();
            _webDriver.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}



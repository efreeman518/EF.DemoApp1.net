using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Endpoints;

/// <summary>
/// Testing http endpoints (MVC controllers, razor pages)
/// </summary>
[TestClass]
public abstract class EndpointTestBase
{
    protected IConfigurationRoot _config;

    protected EndpointTestBase()
    {
        _config = Utility.GetConfiguration();
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Utility;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test.Endpoints;

/// <summary>
/// Testing http endpoints (MVC controllers, razor pages)
/// </summary>
[TestClass]
public abstract class EndpointTestBase
{
    protected static readonly IConfigurationRoot _config = Utility.GetConfiguration();

    protected EndpointTestBase()
    {
    }

    protected async static Task ApplyBearerAuthHeader(HttpClient client)
    {
        var authResult = await AuthTokenProvider.GetAuthResultClient(
               $"{_config.GetValue<string>("Auth:Instance")}{_config.GetValue<string>("Auth:Tenant")}",
               _config.GetValue<string>("ClientId")!,
               _config.GetValue<string>("ClientSecret")!,
               new string[] { _config.GetValue<string>("Auth:Scope")! }); //auth scope in service with "/.default"
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult?.AccessToken);
    }
}

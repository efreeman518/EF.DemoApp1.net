using AngleSharp.Html.Dom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System.Net;

namespace Test.Endpoints.Basic;

[TestClass]
public class BasicEndpointsTests : EndpointTestBase
{
    private const string FACTORY_KEY = "BasicEndpointsTests";
    private static HttpClient _client = null!;

    //html endpoints return success and correct content type
    [DataTestMethod]
    [DataRow("swagger", HttpStatusCode.OK, "text/html; charset=utf-8")]
    [DataRow("index.html", HttpStatusCode.OK, "text/html")]
    public async Task Get_BasicEndpoints_pass(string url, HttpStatusCode expectedStatusCode, string contentType)
    {
        // Act
        (HttpResponseMessage httpResponse, _) = await _client.HttpRequestAndResponseAsync<IHtmlDocument>(HttpMethod.Get, url, null);

        // Assert
        httpResponse.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);
        Assert.AreEqual(contentType, httpResponse.Content.Headers.ContentType?.ToString());
    }

    //reset db after each test
    [TestCleanup]
    public async Task TestCleanup() => await ApiFactoryManager.ResetDatabaseAsync<Program>(FACTORY_KEY);

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.WriteLine(testContext.TestName);

        await ApiFactoryManager.StartDbContainerAsync<Program>(FACTORY_KEY);

        //Arrange for all tests in this class
        _client = ApiFactoryManager.GetClient<Program>(FACTORY_KEY);

        await ApiFactoryManager.InitializeRespawnerAsync<Program>(FACTORY_KEY);

        //Authentication
        //await ApplyBearerAuthHeader(_client);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await ApiFactoryManager.StopDbContainerAsync<Program>(FACTORY_KEY);
        ApiFactoryManager.Cleanup<Program>(FACTORY_KEY);
    }
}

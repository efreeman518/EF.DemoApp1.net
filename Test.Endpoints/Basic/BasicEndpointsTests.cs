using AngleSharp.Html.Dom;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System.Net;

namespace Test.Endpoints.Basic;

[TestClass]
[DoNotParallelize]
public class BasicEndpointsTests : EndpointTestBase
{
    //html endpoints return success and correct content type
    [DataTestMethod]
    [DataRow("swagger", HttpStatusCode.OK, "text/html; charset=utf-8")]
    [DataRow("index.html", HttpStatusCode.OK, "text/html")]
    public async Task Get_BasicEndpoints_pass(string url, HttpStatusCode expectedStatusCode, string contentType)
    {
        // Act
        (HttpResponseMessage httpResponse, _) = await HttpClientApi.HttpRequestAndResponseAsync<IHtmlDocument>(HttpMethod.Get, url, null);

        // Assert
        httpResponse.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);
        Assert.AreEqual(contentType, httpResponse.Content.Headers.ContentType?.ToString());
    }

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.Write($"Start {testContext.TestName}");

        await ConfigureTestInstanceAsync(testContext.TestName!);

        //check api auth configuration; "AzureAd" is in the api config settings
        if (Config.GetSection("AzureAd").Exists()) await ApplyBearerAuthHeaderAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await BaseClassCleanup();
    }
}

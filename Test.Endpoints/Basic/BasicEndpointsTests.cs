﻿using AngleSharp.Html.Dom;
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
    [DataRow("scalar/v1.1", HttpStatusCode.OK, "text/html")]
    //[DataRow("crud.html", HttpStatusCode.OK, "text/html")]
    public async Task Get_BasicEndpoints_pass(string url, HttpStatusCode expectedStatusCode, string contentType)
    {
        var httpClient = await GetHttpClient();

        // Act
        (HttpResponseMessage httpResponse, _) = await httpClient.HttpRequestAndResponseAsync<IHtmlDocument>(HttpMethod.Get, url, null);

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
    }

    [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
    public static async Task ClassCleanup()
    {
        await BaseClassCleanup();
    }
}

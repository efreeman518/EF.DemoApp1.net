using AngleSharp.Html.Dom;
using Application.Contracts.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test.Endpoints.Controller;

[TestClass]
public class TodoControllerTests : EndpointTestBase
{
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.WriteLine(testContext.TestName);

        //Arrange for all tests
        _client = Utility.GetClient<SampleApp.Api.Program>();

        //Authentication
        //await ApplyBearerAuthHeader(_client);
        await Task.CompletedTask; //compiler warning
    }

    //html endpoints return success and correct content type
    [DataTestMethod]
    [DataRow("swagger", HttpStatusCode.OK, "text/html; charset=utf-8")]
    [DataRow("index.html", HttpStatusCode.OK, "text/html")]
    public async Task Get_BasicEndpoints_pass(string url, HttpStatusCode expectedStatusCode, string contentType)
    {
        // Act
        (HttpResponseMessage httpResponse, _) = await _client.HttpRequestAndResponseAsync<string, IHtmlDocument>(HttpMethod.Get, url, null);

        // Assert
        httpResponse.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);
        Assert.AreEqual(contentType, httpResponse.Content.Headers.ContentType?.ToString());
    }

    [TestMethod]
    public async Task CRUD_pass()
    {
        //arrange
        string urlBase = "api/v1/todoitems";
        string name = $"Todo-a-{Guid.NewGuid()}";
        var todo = new TodoItemDto
        {
            Name = name
        };

        //act

        //POST create (insert)
        (var _, var parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto, TodoItemDto>(HttpMethod.Post, urlBase, todo);
        todo = parsedResponse;
        Assert.IsNotNull(todo);

        if (!Guid.TryParse(todo!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsTrue(id != Guid.Empty);

        //GET retrieve
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(id, parsedResponse?.Id);

        //PUT update
        todo.Name = $"Update {name}";
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto, TodoItemDto>(HttpMethod.Put, $"{urlBase}/{id}", todo);
        Assert.AreEqual(todo.Name, parsedResponse?.Name);

        //GET retrieve
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(todo.Name, parsedResponse?.Name);

        //DELETE
        (var httpResponse, _) = await _client.HttpRequestAndResponseAsync<object, object>(HttpMethod.Delete, $"{urlBase}/{id}", null);
        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);

        //GET (NotFound) - ensure deleted
        (httpResponse, _) = await _client.HttpRequestAndResponseAsync<object, TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null, null, false);
        Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);

    }
}

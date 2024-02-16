using Application.Contracts.Model;
using Domain.Shared.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System.Net;

namespace Test.Endpoints.Controller;

[TestClass]
public class TodoControllerTests : EndpointTestBase
{
    private const string FACTORY_KEY = "TodoControllerTests";
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.WriteLine(testContext.TestName);

        await Utility.StartDbContainerAsync<Program>(FACTORY_KEY);

        //Arrange for all tests
        _client = Utility.GetClient<Program>(FACTORY_KEY);

        //Authentication
        //await ApplyBearerAuthHeader(_client);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await Utility.StopDbContainerAsync<Program>(FACTORY_KEY);
        Utility.Cleanup<Program>(FACTORY_KEY);
    }

    [TestMethod]
    public async Task CRUD_pass()
    {
        //arrange
        string urlBase = "api/v1/todoitems";
        string name = $"Todo-a-{Guid.NewGuid()}";
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //act

        //POST create (insert)
        (var _, var parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Post, urlBase, todo);
        todo = parsedResponse;
        Assert.IsNotNull(todo);

        if (!Guid.TryParse(todo!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsTrue(id != Guid.Empty);

        //GET retrieve
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(id, parsedResponse?.Id);

        //PUT update
        var todo2 = todo with { Name = $"Update {name}" };
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Put, $"{urlBase}/{id}", todo2);
        Assert.AreEqual(todo2.Name, parsedResponse?.Name);

        //GET retrieve
        (_, parsedResponse) = await _client.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(todo2.Name, parsedResponse?.Name);

        //DELETE
        (var httpResponse, _) = await _client.HttpRequestAndResponseAsync<object>(HttpMethod.Delete, $"{urlBase}/{id}", null);
        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);

        //GET (NotFound) - ensure deleted
        (httpResponse, _) = await _client.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null, null, false, false);
        Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);

    }
}

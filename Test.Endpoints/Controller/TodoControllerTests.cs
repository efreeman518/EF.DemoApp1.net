using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using System.Net;
using Test.Support;

//parallel to the same api can cause intermittent failures since the api & all tests are using the same database;
//all will try to create the DB if it doesn't exist
//[assembly: DoNotParallelize]

namespace Test.Endpoints.Controller;

[TestClass]
public class TodoControllerTests : EndpointTestBase
{
    private static readonly string? DBSnapshotName = TestConfigSection.GetValue<string?>("DBSnapshotName", null);

    [TestMethod]
    [DoNotParallelize]
    public async Task CRUD_pass()
    {
        //arrange - configure any test data for this test (optional, after snapshot)
        bool respawn = string.IsNullOrEmpty(DBSnapshotName);
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        //generate another 5 completed items
        seedFactories.Add(() => DbContext.SeedEntityData(size: 5, status: TodoItemStatus.Completed));
        //add a single item
        seedFactories.Add(() => DbContext.Add(new TodoItem($"a-{Guid.NewGuid()}") { CreatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow }));
        //grab the seed paths for this test (can't duplicate snapshot)
        List<string>? seedPaths = respawn ? [TestConfigSection.GetValue<string>("SeedFilePath")] : null;
        //reset the DB with the seed scripts & data
        //existing sql db can reset db using snapshot created in ClassInitialize
        await ResetDatabaseAsync(respawn, DBSnapshotName, seedPaths, seedFactories);

        string urlBase = "api/v1/todoitems";
        string name = $"Todo-a-{Guid.NewGuid()}";
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //act

        //POST create (insert)
        (var _, var parsedResponse) = await ApiHttpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Post, urlBase, todo);
        todo = parsedResponse;
        Assert.IsNotNull(todo);

        if (!Guid.TryParse(todo!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsTrue(id != Guid.Empty);

        //GET retrieve
        (_, parsedResponse) = await ApiHttpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(id, parsedResponse?.Id);

        //PUT update
        var todo2 = todo with { Name = $"Update {name}" };
        (_, parsedResponse) = await ApiHttpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Put, $"{urlBase}/{id}", todo2);
        Assert.AreEqual(todo2.Name, parsedResponse?.Name);

        //GET retrieve
        (_, parsedResponse) = await ApiHttpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null);
        Assert.AreEqual(todo2.Name, parsedResponse?.Name);

        //DELETE
        (var httpResponse, _) = await ApiHttpClient.HttpRequestAndResponseAsync<object>(HttpMethod.Delete, $"{urlBase}/{id}", null);
        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);

        //GET (NotFound) - ensure deleted
        (httpResponse, _) = await ApiHttpClient.HttpRequestAndResponseAsync<TodoItemDto>(HttpMethod.Get, $"{urlBase}/{id}", null, null, false, false);
        Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.Write($"Start {testContext.TestName}");
        await ConfigureTestInstanceAsync(testContext.TestName!);

        //existing sql db can reset db using snapshot created in ClassInitialize
        if (TestConfigSection.GetValue<bool>("DBSnapshotCreate") && !string.IsNullOrEmpty(DBSnapshotName))
        {
            List<Action> seedFactories = [() => DbContext.SeedEntityData()];
            seedFactories.Add(() => DbContext.SeedEntityData(size: 5, status: TodoItemStatus.Completed));
            seedFactories.Add(() => DbContext.Add(new TodoItem("a12345") { CreatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow }));
            List<string>? seedPaths = [TestConfigSection.GetValue<string>("SeedFilePath")];
            await ResetDatabaseAsync(true, seedPaths: seedPaths, seedFactories: seedFactories);
            await CreateDbSnapshot(DBSnapshotName);
        }
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        //existing sql db can reset db using snapshot created in ClassInitialize
        if (TestConfigSection.GetValue<bool>("DBSnapshotCreate") && !string.IsNullOrEmpty(DBSnapshotName))
            await DeleteDbSnapshot(DBSnapshotName);

        await BaseClassCleanup();
    }
}

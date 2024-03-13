using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;
using Test.Support;

namespace Test.Integration.Application;

[TestClass]
public class TodoServiceTests : DbIntegrationTestBase
{
    [TestMethod]
    public async Task Todo_CRUD_pass()
    {
        Logger.InfoLog("Starting Todo_CRUD_pass");

        //arrange
        //configure any test data for this test
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        //generate another 5 completed items
        seedFactories.Add(() => DbContext.SeedEntityData(size: 5, status: TodoItemStatus.Completed));
        //add a single item
        seedFactories.Add(() => DbContext.Add(new TodoItem("a12345") { CreatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow }));
        List<string>? seedPaths = [.. TestConfigSection.GetSection("SeedFiles:Paths").Get<string[]>() ?? null];
        await ResetDatabaseAsync(true, seedFactories, seedPaths);


        string name = $"Entity a {Guid.NewGuid()}";
        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(null, name, TodoItemStatus.Created);

        //act & assert

        //create
        var result = await svc.AddItemAsync(todo);
        _ = result.Match(
            dto => todo = dto,
            err => null
        );
        Assert.IsTrue(todo.Id != Guid.Empty);
        Guid id = (Guid)todo.Id!;

        //retrieve
        todo = await svc.GetItemAsync(id);
        Assert.AreEqual(id, todo?.Id);

        //update
        string newName = "mow lawn";
        var todo2 = todo! with { Name = newName, Status = TodoItemStatus.Completed };
        TodoItemDto? updated = null;
        result = await svc.UpdateItemAsync(todo2);
        _ = result.Match(
            dto => updated = dto,
            err => null
        );
        Assert.AreEqual(TodoItemStatus.Completed, updated!.Status);
        Assert.AreEqual(newName, updated?.Name);

        //retrieve and make sure the update persisted
        todo = await svc.GetItemAsync(id);
        Assert.AreEqual(updated!.Status, todo!.Status);

        //delete
        await svc.DeleteItemAsync(id);

        //ensure null after delete
        todo = await svc.GetItemAsync(id);
        Assert.IsNull(todo);
    }

    [DataTestMethod]
    [DataRow("asg")]
    [DataRow("sdfg")]
    [DataRow("sdfgsd456yrt")]
    [DataRow("sdfgs")]
    public async Task Todo_AddItem_fail(string name)
    {
        Logger.InfoLog("Starting Todo_AddItem_fail");

        //arrange
        //configure any test data for this test
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        List<string>? seedPaths = [.. TestConfigSection.GetSection("SeedFiles:Paths").Get<string[]>() ?? null];
        await ResetDatabaseAsync(true, seedFactories, seedPaths);

        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(Guid.Empty, name, TodoItemStatus.Created);

        //act & assert

        //create
        var result = await svc.AddItemAsync(todo);
        Assert.IsTrue(result.IsFaulted);
    }

    /// <summary>
    /// run once at class initialization
    /// </summary>
    /// <param name="testContext"></param>
    /// <returns></returns>
    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        _ = testContext.GetHashCode();
        await StartContainerAsync();
        await ConfigureTestInstanceAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await StopContainerAsync();
    }
}

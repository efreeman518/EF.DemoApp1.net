﻿using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common.Extensions;
using Test.Support;

namespace Test.Integration.Application;

//all tests in this class are using the same database so [DoNotParallelize]

[TestClass]
public class TodoServiceTests : DbIntegrationTestBase
{
    private static readonly string? DBSnapshotName = TestConfigSection.GetValue<string?>("DBSnapshotName", null);

    //Some services under test are injected with IBackgroundTaskQueue which runs in a background thread
    //To prevent these tests from terminating prior to background task completion, at the end of the test,
    //we await a TaskCompletionSource that completes from within the last queued workitem at the end of the test.
    private static readonly BackgroundTaskService _bgTaskService = (BackgroundTaskService)Services.GetRequiredService<IHostedService>();
    private static readonly IBackgroundTaskQueue _bgTaskQueue = Services.GetRequiredService<IBackgroundTaskQueue>();
    private static readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [TestMethod]
    [DoNotParallelize]
    public async Task Todo_CRUD_pass()
    {
        Logger.InfoLog("Starting Todo_CRUD_pass");

        await _bgTaskService!.StartAsync(new CancellationToken());

        //arrange - configure any test data for this test (optional, after snapshot)
        bool respawn = string.IsNullOrEmpty(DBSnapshotName);
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        //generate another 5 completed items
        seedFactories.Add(() => DbContext.SeedEntityData(size: 5, status: TodoItemStatus.Completed));
        //add a single item
        seedFactories.Add(() => DbContext.Add(new TodoItem("a123456")));
        //grab the seed paths for this test (can't duplicate snapshot)
        List<string>? seedPaths = respawn ? [TestConfigSection.GetValue<string>("SeedFilePath")] : null;
        //reset the DB with the seed scripts & data
        //existing sql db can reset db using snapshot created in ClassInitialize
        await ResetDatabaseAsync(respawn, DBSnapshotName, seedPaths, seedFactories);

        string name = $"Entity a {Guid.NewGuid()}";
        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(null, name, TodoItemStatus.Created);

        //act & assert

        //create
        var result = await svc.CreateItemAsync(todo);
        _ = result.Match(
            dto => todo = dto,
            err => throw err

        );
        Assert.IsTrue(todo.Id != Guid.Empty);
        Guid id = (Guid)todo.Id!;

        //retrieve
        var option = await svc.GetItemAsync(id);
        todo = option.Match(
            dto => dto,
            () => throw new AssertFailedException("Item not found")
        );
        Assert.AreEqual(id, todo?.Id);

        //update
        string newName = "mow lawn";
        var todo2 = todo! with { Name = newName, Status = TodoItemStatus.Completed };
        TodoItemDto? updated = null;
        result = await svc.UpdateItemAsync(todo2);
        _ = result.Match(
            dto => updated = dto,
            err => throw err
        );
        Assert.AreEqual(TodoItemStatus.Completed, updated!.Status);
        Assert.AreEqual(newName, updated?.Name);

        //retrieve and make sure the update persisted
        option = await svc.GetItemAsync(id);
        todo = option.Match(
            dto => dto,
            () => throw new AssertFailedException("Item not found")
        );
        Assert.AreEqual(updated!.Status, todo!.Status);

        //delete
        await svc.DeleteItemAsync(id);

        //ensure null after delete
        option = await svc.GetItemAsync(id);
        Assert.IsTrue(option.IsNone);

        //queue the task to complete the test; this enables the test to wait for the background tasks to complete
        _bgTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            await Task.CompletedTask;
            _tcs.SetResult(true);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await _tcs.Task;
    }

    [DataTestMethod]
    [DoNotParallelize]
    [DataRow("asg")]
    [DataRow("sdfg")]
    [DataRow("sdfgsd456yrt")]
    [DataRow("sdfgs")]
    public async Task Todo_AddItem_fail(string name)
    {
        Logger.InfoLog("Starting Todo_AddItem_fail");

        //arrange

        //arrange - configure any test data for this test (optional, after snapshot)
        bool respawn = string.IsNullOrEmpty(DBSnapshotName);
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        List<string>? seedPaths = respawn ? [TestConfigSection.GetValue<string>("SeedFilePath")] : null; //can't duplicate snapshot data Ids
        await ResetDatabaseAsync(respawn, DBSnapshotName, seedPaths, seedFactories);

        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(Guid.Empty, name, TodoItemStatus.Created);

        //act & assert

        //create
        var result = await svc.CreateItemAsync(todo);
        Assert.IsTrue(result.IsFaulted);
    }

    /// <summary>
    /// run once at class/assembly initialization
    /// </summary>
    /// <param name="testContext"></param>
    /// <returns></returns>
    //[AssemblyInitialize]
    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.Write($"Start {testContext.FullyQualifiedTestClassName}");
        await ConfigureTestInstanceAsync(testContext.FullyQualifiedTestClassName!);

        //existing sql db can reset db using snapshot created in ClassInitialize with specific data for this test class/assembly
        if (TestConfigSection.GetValue<bool>("DBSnapshotCreate") && !string.IsNullOrEmpty(DBSnapshotName))
        {
            List<Action> seedFactories = [() => DbContext.SeedEntityData()];
            seedFactories.Add(() => DbContext.SeedEntityData(size: 5, status: TodoItemStatus.Completed));
            seedFactories.Add(() => DbContext.Add(new TodoItem("a12345")));
            List<string>? seedPaths = [TestConfigSection.GetValue<string>("SeedFilePath")];
            await ResetDatabaseAsync(true, seedPaths: seedPaths, seedFactories: seedFactories);
            await CreateDbSnapshot(DBSnapshotName);
        }
    }

    //[AssemblyCleanup]
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        //existing sql db can reset db using snapshot created in ClassInitialize
        if (TestConfigSection.GetValue<bool>("DBSnapshotCreate") && !string.IsNullOrEmpty(DBSnapshotName))
            await DeleteDbSnapshot(DBSnapshotName);

        if (TestConfigSection.GetValue<string?>("DBSource", null) == "TestContainer")
        {
            await BaseClassCleanup();
        }
    }
}

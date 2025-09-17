using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private static readonly ChannelBackgroundTaskService _bgTaskService = (ChannelBackgroundTaskService)Services.GetRequiredService<IHostedService>();
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
        List<string>? seedPaths = null;
        if (respawn)
        {
            var seedFilePath = TestConfigSection.GetValue<string>("SeedFilePath");
            if (seedFilePath != null)
                seedPaths = [seedFilePath];
        }
        //reset the DB with the seed scripts & data
        //existing sql db can reset db using snapshot created in ClassInitialize
        await ResetDatabaseAsync(respawn, DBSnapshotName, seedPaths, seedFactories);

        string name = $"Entity a {Guid.NewGuid()}";
        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(null, name, TodoItemStatus.Created);

        //act & assert

        // Create
        var result = await svc.CreateItemAsync(todo);
        Assert.IsTrue(result.IsSuccess, $"Create failed: {string.Join(",", result.Errors)}");
        todo = result.Value;
        Assert.IsNotNull(todo);
        Assert.IsTrue(todo.Id != Guid.Empty);
        Guid id = (Guid)todo.Id!;

        // Retrieve
        var getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsSuccess, $"Retrieve failed: {string.Join(",", getResult.Errors)}");
        todo = getResult.Value;
        Assert.AreEqual(id, todo?.Id);

        // Update
        string newName = "mow lawn";
        var todo2 = todo! with { Name = newName, Status = TodoItemStatus.Completed };
        var updateResult = await svc.UpdateItemAsync(todo2);
        Assert.IsTrue(updateResult.IsSuccess, $"Update failed: {string.Join(",", updateResult.Errors)}");
        var updated = updateResult.Value;
        Assert.AreEqual(TodoItemStatus.Completed, updated!.Status);
        Assert.AreEqual(newName, updated?.Name);

        // Retrieve and ensure the update persisted
        getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsSuccess, $"Retrieve after update failed: {string.Join(",", getResult.Errors)}");
        todo = getResult.Value;
        Assert.AreEqual(updated!.Status, todo!.Status);

        //delete
        await svc.DeleteItemAsync(id);

        // Ensure null after delete
        getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsNone, "Item was not deleted");

        //queue the task to complete the test; this enables the test to wait for the background tasks to complete
        _bgTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            await Task.CompletedTask;
            _tcs.SetResult(true);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await _tcs.Task;
    }

    [TestMethod]
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
        List<string>? seedPaths = null;
        if (respawn)
        {
            var seedFilePath = TestConfigSection.GetValue<string>("SeedFilePath");
            if (seedFilePath != null)
                seedPaths = [seedFilePath];
        } //can't duplicate snapshot data Ids
        await ResetDatabaseAsync(respawn, DBSnapshotName, seedPaths, seedFactories);

        TodoService svc = (TodoService)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new(Guid.Empty, name, TodoItemStatus.Created);

        //act & assert

        // Create
        var result = await svc.CreateItemAsync(todo);
        Assert.IsFalse(result.IsSuccess, "Expected failure but got success");
        Assert.IsNotNull(string.Join(",", result.Errors), "Expected an error message but got none");
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
            var seedFilePath = TestConfigSection.GetValue<string>("SeedFilePath");
            List<string>? seedPaths = seedFilePath != null ? [seedFilePath] : null;
            await ResetDatabaseAsync(true, seedPaths: seedPaths, seedFactories: seedFactories);
            await CreateDbSnapshot(DBSnapshotName);
        }
    }

    //[AssemblyCleanup]
    [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
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

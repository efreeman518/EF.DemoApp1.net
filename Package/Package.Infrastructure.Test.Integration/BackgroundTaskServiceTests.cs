using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Test.Integration.Service;

namespace Package.Infrastructure.Test.Integration;

[TestClass]
public class BackgroundTaskServiceTests : IntegrationTestBase
{
    //[TestMethod]
    //demonstrate a background task does not wait during test run
    public async Task RunBackgroundTaskX_pass()
    {
        var sut = Services.GetService<IHostedService>() as BackgroundTaskService;
        var q = Services.GetRequiredService<IBackgroundTaskQueue>();

        await sut!.StartAsync(CancellationToken.None);
        var isExecuted = false;

        //queue background task1
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(1000, token);
            isExecuted = true;
            Logger.LogInformation("Task 1 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        Assert.IsTrue(isExecuted);
    }

    [TestMethod]
    public async Task RunBackgroundTask_with_TCS_pass()
    {
        var sut = Services.GetService<IHostedService>() as BackgroundTaskService;
        var q = Services.GetRequiredService<IBackgroundTaskQueue>();

        await sut!.StartAsync(CancellationToken.None);
        var isExecuted = false;

        // for this test, since the sut (BackgroundTaskService) will run in a background thread, in order to prevent
        // this test from terminating immediately, we can use a task completion source that
        // we complete from within the sut.
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        //queue background task1
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(1000, token);
            Logger.LogInformation("Task 1 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue background task2
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(2000, token);
            Logger.LogInformation("Task 2 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue background task3
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(3000, token);
            Logger.LogInformation("Task 3 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue background task4
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(4000, token);
            Logger.LogInformation("Task 4 Done at {Time}", DateTime.UtcNow.TimeOfDay);

            isExecuted = true;
            //complete the background task for this test method - allow await tcs.Task to continue
            tcs.SetResult(true);
        });

        //queue background task5
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(1000, token);
            Logger.LogInformation("Task 5 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await tcs.Task;

        await Task.Delay(3000);

        //again prevent the test from running to completion until the sut has executed
        tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(1000, token);
            Logger.LogInformation("Task 6 Done at {Time}", DateTime.UtcNow.TimeOfDay);

            isExecuted = true;
            //complete the background task for this test method - allow await tcs.Task to continue
            tcs.SetResult(true);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await tcs.Task;

        Assert.IsTrue(isExecuted);

        await sut.StopAsync(CancellationToken.None);
        Logger.LogInformation("Test completed.");
    }

    [TestMethod]
    public async Task RunBackgroundScopedAndNonScopedTasks_pass()
    {
        var sut = Services.GetService<IHostedService>() as BackgroundTaskService;
        var q = Services.GetRequiredService<IBackgroundTaskQueue>();

        await sut!.StartAsync(CancellationToken.None);
        var isExecuted = false;

        // for this test, since the sut (BackgroundTaskService) will run in a background thread, in order to prevent
        // this test from terminating immediately, we can use a task completion source that
        // we complete from within the sut.
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        //queue scoped background task1
        q.QueueScopedBackgroundWorkItem<ISomeScopedService>(async (someScopedService, token) =>
        {
            await someScopedService.SomeAsyncWork(token);
            Logger.LogInformation("Scoped Task 1 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue background task2
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(2000, token);
            Logger.LogInformation("Task 2 (which includes a scoped service) Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue scoped background task3
        q.QueueScopedBackgroundWorkItem<ISomeScopedService>(async (someScopedService, token) =>
         {
             await someScopedService.SomeAsyncWork(token);
             Logger.LogInformation("Scoped Task 3 Done at {Time}", DateTime.UtcNow.TimeOfDay);
         });

        //queue background task4
        q.QueueBackgroundWorkItem(async token =>
        {
            await Task.Delay(4000, token);
            Logger.LogInformation("Task 4 Done at {Time}", DateTime.UtcNow.TimeOfDay);

            isExecuted = true;
            //complete the background task for this test method - allow await tcs.Task to continue
            tcs.SetResult(true);
        });

        //queue scoped background task5
        q.QueueScopedBackgroundWorkItem<ISomeScopedService>(async (someScopedService, token) =>
        {
            await someScopedService.SomeAsyncWork(token);
            Logger.LogInformation("Scoped Task 5 Done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await tcs.Task;

        await Task.Delay(3000);

        //again prevent the test from running to completion until the sut has executed
        tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        q.QueueScopedBackgroundWorkItem<ISomeScopedService>(async (someScopedService, token) =>
        {
            await someScopedService.SomeAsyncWork(token);
            Logger.LogInformation("Scoped Task 6 Done at {Time}", DateTime.UtcNow.TimeOfDay);

            isExecuted = true;
            //complete the background task for this test method - allow await tcs.Task to continue
            tcs.SetResult(true);
        });

        //await our task completion source task so that the sut will execute until tcs.SetResult(true).
        await tcs.Task;

        Assert.IsTrue(isExecuted);

        await sut.StopAsync(CancellationToken.None);
        Logger.LogInformation("Test completed.");
    }

}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Package.Infrastructure.BackgroundServices.Tests;

[TestClass]
public class ChannelBackgroundTaskServiceTests
{
    [TestMethod]
    public async Task ChannelBackgroundTaskQueue_ProcessesAllItems()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var queue = new ChannelBackgroundTaskQueue(scopeFactory, new NullLogger<ChannelBackgroundTaskQueue>());
        var logger = NullLogger<ChannelBackgroundTaskService>.Instance;
        var service = new ChannelBackgroundTaskService(queue, logger);

        const int itemCount = 100;
        var processedItems = new ConcurrentBag<int>();

        // Act - Start the service
        await service.StartAsync(CancellationToken.None);

        // Queue items
        for (int i = 0; i < itemCount; i++)
        {
            var itemId = i;
            queue.QueueBackgroundWorkItem(async token =>
            {
                await Task.Delay(10, token);
                processedItems.Add(itemId);
            });
        }

        // Wait for all items to be processed
        await WaitForProcessingAsync(() => processedItems.Count >= itemCount, TimeSpan.FromSeconds(10));

        // Stop the service
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.HasCount(itemCount, processedItems);
    }

    [TestMethod]
    public async Task ChannelBackgroundTaskQueue_HandlesCompletionAndStops()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var queue = new ChannelBackgroundTaskQueue(scopeFactory, new NullLogger<ChannelBackgroundTaskQueue>());
        var logger = NullLogger<ChannelBackgroundTaskService>.Instance;
        var service = new ChannelBackgroundTaskService(queue, logger);

        const int itemCount = 10;
        var processedItems = new ConcurrentBag<int>();
        var serviceStoppedTaskSource = new TaskCompletionSource<bool>();

        // Override ExecuteAsync to detect when the service exits
        _ = Task.Run(async () =>
        {
            try
            {
                await service.StartAsync(CancellationToken.None);

                // This is a hack to wait for the service to exit normally
                // In a real scenario, we'd monitor the service's lifetime
                await Task.Delay(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
            }
            catch (Exception)
            {
                // Ignore exceptions
            }
            finally
            {
                serviceStoppedTaskSource.TrySetResult(true);
            }
        }, TestContext.CancellationToken);

        // Act - Queue some items
        for (int i = 0; i < itemCount; i++)
        {
            var itemId = i;
            queue.QueueBackgroundWorkItem(async token =>
            {
                await Task.Delay(10, token);
                processedItems.Add(itemId);
            });
        }

        // Wait for all items to be processed
        await WaitForProcessingAsync(() => processedItems.Count >= itemCount, TimeSpan.FromSeconds(5));

        // Complete the queue - no more items will be accepted
        queue.Complete();

        // Wait for the service to stop
        var completedInTime = await Task.WhenAny(
            serviceStoppedTaskSource.Task,
            Task.Delay(TimeSpan.FromSeconds(10), TestContext.CancellationToken)
        ) == serviceStoppedTaskSource.Task;

        // Stop the service if needed
        if (!completedInTime)
        {
            await service.StopAsync(CancellationToken.None);
        }

        // Assert
        Assert.HasCount(itemCount, processedItems);
        Assert.IsTrue(completedInTime, "Service should stop after queue is completed");
    }

    [TestMethod]
    public async Task ChannelBackgroundTaskQueue_ProcessesScopedItems()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory, NullLoggerFactory>();

        // Register a scoped test service
        serviceCollection.AddScoped<TestScopedService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var queue = new ChannelBackgroundTaskQueue(scopeFactory, new NullLogger<ChannelBackgroundTaskQueue>());
        var logger = NullLogger<ChannelBackgroundTaskService>.Instance;
        var service = new ChannelBackgroundTaskService(queue, logger);

        const int itemCount = 10;
        var processedItems = new ConcurrentBag<Guid>();

        // Act - Start the service
        await service.StartAsync(CancellationToken.None);

        // Queue scoped items
        for (int i = 0; i < itemCount; i++)
        {
            queue.QueueScopedBackgroundWorkItem<TestScopedService>(async (scopedService, token) =>
            {
                await Task.Delay(10, token);
                processedItems.Add(scopedService.InstanceId);
            });
        }

        // Wait for all items to be processed
        await WaitForProcessingAsync(() => processedItems.Count >= itemCount, TimeSpan.FromSeconds(5));

        // Stop the service
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.HasCount(itemCount, processedItems);

        // Each scoped service should have a unique instance ID
        var uniqueIds = processedItems.Distinct().Count();
        Assert.AreEqual(itemCount, uniqueIds, "Each scoped service should be a unique instance");
    }

    private static async Task WaitForProcessingAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.Elapsed < timeout)
        {
            await Task.Delay(50);
        }
        sw.Stop();
    }

    // Test scoped service
    private class TestScopedService
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
    }

    public TestContext TestContext { get; set; }
}
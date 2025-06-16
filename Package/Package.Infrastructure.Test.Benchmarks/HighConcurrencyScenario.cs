using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.BackgroundServices;

namespace Package.Infrastructure.Test.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class HighConcurrencyScenario
{
    private BackgroundTaskQueue _standardQueue = null!;
    private ChannelBackgroundTaskQueue _channelQueue = null!;

    [Params(1000)]
    public int TaskCount { get; set; }

    // Testing with high numbers of consumers
    [Params(8, 16, 32)]
    public int ConsumerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<ChannelBackgroundTaskQueue>.Instance;

        _standardQueue = new BackgroundTaskQueue(factory);
        _channelQueue = new ChannelBackgroundTaskQueue(factory, logger);
    }

    [Benchmark(Baseline = true)]
    public async Task StandardQueue_HighConcurrency()
    {
        // Create completion source to track when all items are processed
        var completed = new TaskCompletionSource<bool>();
        var processCount = 0;

        // Setup consumer tasks
        var consumerTasks = new List<Task>();
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        for (int i = 0; i < ConsumerCount; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var workItem = await _standardQueue.DequeueAsync(token);
                        if (workItem != null)
                        {
                            await workItem(token);
                            var current = Interlocked.Increment(ref processCount);
                            if (current == TaskCount)
                            {
                                completed.TrySetResult(true);
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }, token));
        }

        // Enqueue tasks with simulated CPU work
        for (int i = 0; i < TaskCount; i++)
        {
            _standardQueue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate both CPU and I/O work
                SimulateCpuWork(0.5); // 0.5ms of CPU work
                await Task.Delay(1, ct); // 1ms of I/O work
            });
        }

        // Wait for all items to be processed or timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
        await Task.WhenAny(completed.Task, timeoutTask);

        await cancellationTokenSource.CancelAsync();
        try
        {
            await Task.WhenAll(consumerTasks.Where(t => !t.IsCompleted));
        }
        catch (OperationCanceledException)
        {
            //handle
        }

        cancellationTokenSource.Dispose();
    }

    [Benchmark]
    public async Task ChannelQueue_HighConcurrency()
    {
        // Create completion source to track when all items are processed
        var completed = new TaskCompletionSource<bool>();
        var processCount = 0;

        // Setup consumer tasks
        var consumerTasks = new List<Task>();
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        for (int i = 0; i < ConsumerCount; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
            {
                await foreach (var workItem in _channelQueue.ReadAllAsync(token))
                {
                    await workItem(token);
                    var current = Interlocked.Increment(ref processCount);
                    if (current == TaskCount)
                    {
                        completed.TrySetResult(true);
                        break;
                    }
                }
            }));
        }

        // Enqueue tasks with simulated CPU work
        for (int i = 0; i < TaskCount; i++)
        {
            _channelQueue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate both CPU and I/O work
                SimulateCpuWork(0.5); // 0.5ms of CPU work
                await Task.Delay(1, ct); // 1ms of I/O work
            });
        }

        // Wait for all items to be processed or timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
        await Task.WhenAny(completed.Task, timeoutTask);

        await cancellationTokenSource.CancelAsync();
        _channelQueue.Complete();
        try
        {
            await Task.WhenAll(consumerTasks.Where(t => !t.IsCompleted));
        }
        catch (OperationCanceledException)
        {
            //handle
        }

        cancellationTokenSource.Dispose();
    }

    private static void SimulateCpuWork(double milliseconds)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.Elapsed.TotalMilliseconds < milliseconds)
        {
            // Busy wait to simulate CPU-bound work
            Thread.SpinWait(1000);
        }
    }
}
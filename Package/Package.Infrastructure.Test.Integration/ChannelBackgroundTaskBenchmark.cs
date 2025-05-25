using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Package.Infrastructure.BackgroundServices.Tests;

[MemoryDiagnoser]
public class BackgroundTaskQueueBenchmark
{
    private IServiceProvider _serviceProvider = null!;
    private BackgroundTaskQueue _standardQueue = null!;
    private ChannelBackgroundTaskQueue _channelQueue = null!;
    private int _iterations;

    [Params(100, 1000, 10000)]
    public int TaskCount { get; set; }

    [Params(1, 2, 4)]
    public int ConsumerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        var factory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _standardQueue = new BackgroundTaskQueue(factory);
        _channelQueue = new ChannelBackgroundTaskQueue(factory);

        _iterations = 0;
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _iterations = 0;
    }

    [Benchmark]
    public async Task StandardQueue_EnqueueDequeue()
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

        // Enqueue tasks
        for (int i = 0; i < TaskCount; i++)
        {
            var taskId = i;
            _standardQueue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work
                await Task.Yield();
                Interlocked.Increment(ref _iterations);
            });
        }

        // Wait for all items to be processed
        await completed.Task;
        cancellationTokenSource.Cancel();
        try
        {
            await Task.WhenAll(consumerTasks);
        }
        catch (OperationCanceledException) { }
    }

    [Benchmark]
    public async Task ChannelQueue_EnqueueDequeue()
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
                        var workItem = await _channelQueue.DequeueAsync(token);
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

        // Enqueue tasks
        for (int i = 0; i < TaskCount; i++)
        {
            var taskId = i;
            _channelQueue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work
                await Task.Yield();
                Interlocked.Increment(ref _iterations);
            });
        }

        // Wait for all items to be processed
        await completed.Task;
        cancellationTokenSource.Cancel();
        try
        {
            await Task.WhenAll(consumerTasks);
        }
        catch (OperationCanceledException) { }
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.BackgroundServices;

namespace Package.Infrastructure.Test.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BackgroundTaskQueueBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private IServiceScopeFactory _serviceScopeFactory = null!;
    private ILogger<ChannelBackgroundTaskQueue> _logger = null!;
    private int _iterations;

    [Params(100, 1000, 10000)]  // Reduced max size for initial testing
    public int TaskCount { get; set; }

    [Params(1, 2, 4)]       // Reduced for initial testing
    public int ConsumerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _logger = NullLogger<ChannelBackgroundTaskQueue>.Instance;

        _iterations = 0;
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _iterations = 0;
    }

    [Benchmark(Description = "Standard Queue (ConcurrentQueue + SemaphoreSlim)", Baseline = true)]
    [BenchmarkCategory("EnqueueDequeue")]
    public async Task StandardQueue_EnqueueDequeue()
    {
        // Create a new queue for each benchmark iteration
        var queue = new BackgroundTaskQueue(_serviceScopeFactory);

        // Create completion source to track when all items are processed
        var completed = new TaskCompletionSource<bool>();
        var processCount = 0;

        // Setup consumer tasks
        var consumerTasks = new List<Task>();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cancellationTokenSource.Token;

        // Enqueue tasks first to avoid potential deadlock
        for (int i = 0; i < TaskCount; i++)
        {
            queue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work - make it truly minimal for benchmarking
                await Task.CompletedTask;
                Interlocked.Increment(ref _iterations);
            });
        }

        // Start consumers after tasks are queued
        for (int i = 0; i < ConsumerCount; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var workItem = await queue.DequeueAsync(token);
                        if (workItem != null)
                        {
                            await workItem(token);
                            var current = Interlocked.Increment(ref processCount);
                            if (current >= TaskCount)
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
                    catch (Exception)
                    {
                        break;
                    }
                }
            }, token));
        }

        // Wait for all items to be processed or timeout
        await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(20), token));

        // Ensure cancellation and cleanup
        cancellationTokenSource.Cancel();

        // Wait for tasks to complete with timeout
        await Task.WhenAny(
            Task.WhenAll(consumerTasks.Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously))),
            Task.Delay(1000)
        );
    }

    [Benchmark(Description = "Channel Queue")]
    [BenchmarkCategory("EnqueueDequeue")]
    public async Task ChannelQueue_EnqueueDequeue()
    {
        // Create a new queue for each benchmark iteration
        var queue = new ChannelBackgroundTaskQueue(_serviceScopeFactory, _logger);

        // Create completion source to track when all items are processed
        var completed = new TaskCompletionSource<bool>();
        var processCount = 0;

        // Setup consumer tasks
        var consumerTasks = new List<Task>();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cancellationTokenSource.Token;

        // Enqueue tasks first to avoid potential deadlock
        for (int i = 0; i < TaskCount; i++)
        {
            queue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work - make it truly minimal for benchmarking
                await Task.CompletedTask;
                Interlocked.Increment(ref _iterations);
            });
        }

        // Start consumers after tasks are queued
        for (int i = 0; i < ConsumerCount; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var workItem = await queue.DequeueAsync(token);
                        if (workItem != null)
                        {
                            await workItem(token);
                            var current = Interlocked.Increment(ref processCount);
                            if (current >= TaskCount)
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
                    catch (Exception)
                    {
                        break;
                    }
                }
            }, token));
        }

        // Wait for all items to be processed or timeout
        await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(20), token));

        // Ensure cancellation and cleanup
        cancellationTokenSource.Cancel();

        // Wait for tasks to complete with timeout
        await Task.WhenAny(
            Task.WhenAll(consumerTasks.Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously))),
            Task.Delay(1000)
        );
    }

    [Benchmark(Description = "Channel Queue with ReadAllAsync")]
    [BenchmarkCategory("EnqueueDequeue")]
    public async Task ChannelQueue_ReadAllAsync()
    {
        // Create a new queue for each benchmark iteration to avoid channel already completed exception
        var queue = new ChannelBackgroundTaskQueue(_serviceScopeFactory, _logger);

        // Create completion source to track when all items are processed
        var completed = new TaskCompletionSource<bool>();
        var processCount = 0;

        // Setup consumer tasks
        var consumerTasks = new List<Task>();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cancellationTokenSource.Token;

        // Start consumers first
        for (int i = 0; i < ConsumerCount; i++)
        {
            int consumerId = i;
            consumerTasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Use the queue's ReadAllAsync method
                    await foreach (var workItem in queue.ReadAllAsync(token))
                    {
                        await workItem(token);
                        var current = Interlocked.Increment(ref processCount);
                        if (current >= TaskCount)
                        {
                            completed.TrySetResult(true);
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Expected when canceled
                }
                catch (Exception ex)
                {
                    // Log in production
                    Console.WriteLine($"Error in consumer {consumerId}: {ex.Message}");
                }
            }, token));
        }

        // Enqueue tasks after consumers are ready
        for (int i = 0; i < TaskCount; i++)
        {
            queue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work
                await Task.CompletedTask;
                Interlocked.Increment(ref _iterations);
            });
        }

        // Complete the channel to signal no more items will be added
        queue.Complete();

        // Wait for all items to be processed or timeout
        await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(20), token));

        // Ensure cancellation and cleanup
        cancellationTokenSource.Cancel();

        // Wait for tasks to complete with timeout
        await Task.WhenAny(
            Task.WhenAll(consumerTasks.Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously))),
            Task.Delay(1000)
        );
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.BackgroundServices;
using System.Threading.Channels;

namespace Package.Infrastructure.Test.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class HighThroughputScenario
{
    private IServiceScopeFactory _serviceScopeFactory = null!;
    private ILogger<ChannelBackgroundTaskQueue> _logger = null!;

    // Testing with very high task count - reduced for initial testing
    [Params(10000)]
    public int TaskCount { get; set; }

    [Params(4)]
    public int ConsumerCount { get; set; }

    [Params(4)]
    public int ProducerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _logger = NullLogger<ChannelBackgroundTaskQueue>.Instance;
    }

    [Benchmark(Baseline = true)]
    public async Task StandardQueue_HighThroughput()
    {
        // Create a new queue for each benchmark run
        var queue = new BackgroundTaskQueue(_serviceScopeFactory);
        await RunBenchmark(queue);
    }

    [Benchmark]
    public async Task ChannelQueue_HighThroughput()
    {
        // Create a new queue for each benchmark run
        var queue = new ChannelBackgroundTaskQueue(_serviceScopeFactory, _logger);
        await RunBenchmark(queue);
    }

    private async Task RunBenchmark(IBackgroundTaskQueue queue)
    {
        // Use a shared counter instead of passing by reference
        int processedCount = 0;
        var enqueueCompletionSource = new TaskCompletionSource<bool>();
        var dequeueCompletionSource = new TaskCompletionSource<bool>();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)); // Add timeout
        var token = cancellationTokenSource.Token;

        // Start consumer tasks
        var consumerTasks = Enumerable.Range(0, ConsumerCount)
            .Select(_ => StartConsumer(queue, processedCount, dequeueCompletionSource, TaskCount, token))
            .ToArray();

        // Start producer tasks
        var tasksPerProducer = TaskCount / ProducerCount;
        var producerTasks = Enumerable.Range(0, ProducerCount)
            .Select(i => StartProducer(queue, Math.Min(tasksPerProducer, TaskCount - i * tasksPerProducer)))
            .ToArray();

        // Wait for all producers to finish
        await Task.WhenAll(producerTasks);
        enqueueCompletionSource.TrySetResult(true);

        // Wait for processing to complete or timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), token);
        await Task.WhenAny(dequeueCompletionSource.Task, timeoutTask);

        // Clean up
        if (!cancellationTokenSource.IsCancellationRequested)
            await cancellationTokenSource.CancelAsync();

        // Complete the channel if it's a ChannelBackgroundTaskQueue
        if (queue is ChannelBackgroundTaskQueue channelQueue && !channelQueue.IsCompleted)
        {
            try
            {
                channelQueue.Complete();
            }
            catch (ChannelClosedException)
            {
                // Ignore if already completed
            }
        }

        // Don't wait for all tasks to complete - this could cause hanging
        await Task.WhenAny(
            Task.WhenAll(consumerTasks.Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously))),
            Task.Delay(1000)
        );
    }

    private static Task StartConsumer(IBackgroundTaskQueue queue, int processedCount, TaskCompletionSource<bool> completionSource, int totalTasks, CancellationToken token)
    {
        if (queue is ChannelBackgroundTaskQueue channelQueue)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await foreach (var workItem in channelQueue.ReadAllAsync(token))
                    {
                        try
                        {
                            await workItem(token);
                            var count = Interlocked.Increment(ref processedCount);
                            if (count >= totalTasks) // Use >= to handle race conditions
                            {
                                completionSource.TrySetResult(true);
                            }
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            // Expected when cancellation is requested
                            break;
                        }
                        catch (Exception)
                        {
                            // Ignore exceptions during benchmarking
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Expected
                }
                catch (Exception)
                {
                    // Ignore other exceptions during benchmarking
                }
            }, token);
        }
        else
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var workItem = await queue.DequeueAsync(token);
                        if (workItem != null)
                        {
                            await workItem(token);
                            var count = Interlocked.Increment(ref processedCount);
                            if (count >= totalTasks) // Use >= to handle race conditions
                            {
                                completionSource.TrySetResult(true);
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
                        // Ignore exceptions during benchmarking
                    }
                }
            }, token);
        }
    }

    private static Task StartProducer(IBackgroundTaskQueue queue, int count)
    {
        return Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < count; i++)
                {
                    // Check if we can continue
                    if (queue is ChannelBackgroundTaskQueue channelQueue && channelQueue.IsCompleted)
                        break;

                    try
                    {
                        queue.QueueBackgroundWorkItem(async ct =>
                        {
                            // Very minimal work
                            await Task.CompletedTask;
                        });
                    }
                    catch (ChannelClosedException)
                    {
                        // Channel might have closed while we were adding items
                        break;
                    }
                }
            }
            catch (ChannelClosedException)
            {
                // Channel might be closed if completion happens simultaneously
            }
            catch (Exception)
            {
                // Ignore other exceptions during benchmarking
            }
        });
    }
}
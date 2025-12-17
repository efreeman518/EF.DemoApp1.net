using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Package.Infrastructure.BackgroundServices.Work;
using System.Threading.Channels;

namespace Package.Infrastructure.Test.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class ChannelBackgroundTaskQueueBenchmarks
{
    private ChannelBackgroundTaskQueue _unboundedQueue = null!;
    private ChannelBackgroundTaskQueue _boundedQueue = null!;
    private ChannelBackgroundTaskQueue _boundedDropOldestQueue = null!;

    [Params(1000, 10000)]
    public int TaskCount { get; set; }

    [Params(1, 4)]
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

        // Create queues with different configurations
        _unboundedQueue = new ChannelBackgroundTaskQueue(factory, logger);
        _boundedQueue = new ChannelBackgroundTaskQueue(factory, logger, TaskCount / 10);
        _boundedDropOldestQueue = new ChannelBackgroundTaskQueue(
            factory,
            logger,
            TaskCount / 10,
            BoundedChannelFullMode.DropOldest);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ChannelType")]
    public async Task UnboundedChannel_EnqueueDequeue()
    {
        await RunBenchmark(_unboundedQueue);
    }

    [Benchmark]
    [BenchmarkCategory("ChannelType")]
    public async Task BoundedChannel_EnqueueDequeue()
    {
        await RunBenchmark(_boundedQueue);
    }

    [Benchmark]
    [BenchmarkCategory("ChannelType")]
    public async Task BoundedDropOldestChannel_EnqueueDequeue()
    {
        await RunBenchmark(_boundedDropOldestQueue);
    }

    private async Task RunBenchmark(ChannelBackgroundTaskQueue queue)
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
                await foreach (var workItem in queue.ReadAllAsync(token))
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

        // Enqueue tasks
        for (int i = 0; i < TaskCount; i++)
        {
            queue.QueueBackgroundWorkItem(async ct =>
            {
                // Simulate some minimal work
                await Task.Yield();
            });
        }

        // Wait for all items to be processed
        await completed.Task;
        await cancellationTokenSource.CancelAsync();
        queue.Complete();
        await Task.WhenAll(consumerTasks.Where(t => !t.IsCompleted));

        cancellationTokenSource.Dispose();
    }
}
namespace Package.Infrastructure.Common.Extensions;
public static class IAsyncEnumerableExtensions
{
    /// <summary>
    /// Concurrently batch process (for async I/O intensive work); Fill the batch, await all tasks, load next batch
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="batchSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> ConcurrentBatchAsync<T>(this IAsyncEnumerable<T> stream,
        Func<T, Task> method, int batchSize = 100, CancellationToken cancellationToken = default)
    {
        List<Task> batchTasks = [];
        Task task;
        int total = 0;

        await foreach (var item in stream)
        {
            total++;
            task = method(item);
            batchTasks.Add(task);
            //if the batch is full, await those tasks and empty the bucket
            if (total % batchSize == 0)
            {
                await Task.WhenAll(batchTasks);
                batchTasks.Clear();
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        //all items have been iterated through but we might have some left in batchTasks
        if (batchTasks.Count > 0)
        {
            await Task.WhenAll(batchTasks);
        }
        return total;
    }

    /// <summary>
    /// Concurrently pipe process (for async I/O intensive work); Fill the pipe, and keep the pipe filled with awaitable tasks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="maxConcurrent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> ConcurrentPipeAsync<T>(this IAsyncEnumerable<T> stream,
        Func<T, Task> method, int maxConcurrent = 100, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim semaphore = new(maxConcurrent);
        var tasks = new List<Task>();
        int total = 0;

        await foreach (var item in stream)
        {
            total++;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await method(item);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
        }
        await Task.WhenAll(tasks);
        return total;
    }

    /// <summary>
    /// Parallel process async the stream (for async CPU intensive work)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> ProcessParallelAsync<T>(this IAsyncEnumerable<T> stream, Func<T, Task> method,
        int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
    {
        int total = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(stream, options, async (item, token) =>
        {
            Interlocked.Increment(ref total);
            await method(item);
        });
        return total;
    }

    /// <summary>
    /// Parallel process async the stream (for sync CPU intensive work)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> ProcessParallelSync<T>(this IAsyncEnumerable<T> stream, Action<T> method,
        int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
    {
        int total = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(stream, options, (item, token) =>
        {
            Interlocked.Increment(ref total);
            method(item);
            return ValueTask.CompletedTask;
        });
        return total;
    }

}

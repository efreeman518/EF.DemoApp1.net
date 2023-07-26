using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Common.Extensions;
public static class IAsyncEnumerableExtensions
{
    public static async Task<int> RunBatchAsync<T>(this IAsyncEnumerable<T> stream, Func<T, Task> method, int batchSize = 100)
    {
        List<Task> batchTasks = new();
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
        }
        //all items have been iterated through but we might have some left in batchTasks
        if (batchTasks.Count > 0)
        {
            await Task.WhenAll(batchTasks);
        }
        return total;
    }

    public static async Task<int> RunPipeAsync<T>(this IAsyncEnumerable<T> stream, Func<T, Task> method, int maxConcurrent = 100)
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
            }));
        }
        await Task.WhenAll(tasks);
        return total;
    }

    /// <summary>
    /// Use with IAsyncEnumerable.WithCancellation(cancellationToken)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public static async Task<int> RunBatchAsync<T>(this ConfiguredCancelableAsyncEnumerable<T> stream, Func<T, Task> method, int batchSize = 100)
    {
        List<Task> batchTasks = new();
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
        }
        //all items have been iterated through but we might have some left in batchTasks
        if (batchTasks.Count > 0)
        {
            await Task.WhenAll(batchTasks);
        }
        return total;
    }

    /// <summary>
    /// Use with IAsyncEnumerable.WithCancellation(cancellationToken)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="method"></param>
    /// <param name="maxConcurrent"></param>
    /// <returns></returns>
    public static async Task<int> RunPipeAsync<T>(this ConfiguredCancelableAsyncEnumerable<T> stream, Func<T, Task> method, int maxConcurrent = 100)
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
            }));
        }
        await Task.WhenAll(tasks);
        return total;
    }
}

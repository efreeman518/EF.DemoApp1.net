using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.BackgroundServices;

/// <summary>
/// Extension methods for registering background task services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the original background task queue with ConcurrentQueue and SemaphoreSlim
    /// </summary>
    public static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services)
    {
        //services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        //services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, ChannelBackgroundTaskQueue>();
        services.AddHostedService<ChannelBackgroundTaskService>();
        return services;
    }

    /// <summary>
    /// Adds the channel-based background task queue
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="boundedCapacity">Optional capacity limit for bounded channel</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddChannelBackgroundTaskQueue(this IServiceCollection services, int? boundedCapacity = null)
    {
        services.AddSingleton<ChannelBackgroundTaskQueue>(sp =>
            new ChannelBackgroundTaskQueue(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetService<ILogger<ChannelBackgroundTaskQueue>>(),
                boundedCapacity));

        services.AddSingleton<IBackgroundTaskQueue>(sp =>
            sp.GetRequiredService<ChannelBackgroundTaskQueue>());

        services.AddHostedService<ChannelBackgroundTaskService>();

        return services;
    }

    /// <summary>
    /// Adds a channel background task queue with completion handling on app shutdown
    /// </summary>
    public static IServiceCollection AddChannelBackgroundTaskQueueWithShutdownHandling(
        this IServiceCollection services,
        int? boundedCapacity = null)
    {
        services.AddChannelBackgroundTaskQueue(boundedCapacity);
        services.AddHostedService<ChannelCompletionService>();

        return services;
    }
}
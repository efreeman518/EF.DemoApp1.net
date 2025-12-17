using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.BackgroundServices.Cron;
using Package.Infrastructure.BackgroundServices.Work;

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
        {
            var logger = sp.GetService<ILogger<ChannelBackgroundTaskQueue>>();
            return logger == null
                ? throw new InvalidOperationException("ILogger<ChannelBackgroundTaskQueue> is not registered in the service collection.")
                : new ChannelBackgroundTaskQueue(
                sp.GetRequiredService<IServiceScopeFactory>(),
                logger,
                boundedCapacity);
        });

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

    /// <summary>
    /// Adds a cron job with its handler
    /// </summary>
    /// <typeparam name="TJobSettings">The job settings type</typeparam>
    /// <typeparam name="THandler">The handler type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCronJob<TJobSettings, THandler>(this IServiceCollection services)
        where TJobSettings : CronJobSettings
        where THandler : class, ICronJobHandler<TJobSettings>
    {
        services.AddScoped<ICronJobHandler<TJobSettings>, THandler>();
        services.AddHostedService<CronBackgroundService<TJobSettings>>();
        return services;
    }
}
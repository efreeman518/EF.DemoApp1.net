using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.BackgroundServices;
using SampleApp.BackgroundServices.Scheduler;

namespace SampleApp.BackgroundServices;

/// <summary>
/// Extension methods for registering cron job services
/// </summary>
public static class CronJobServiceRegistration
{
    /// <summary>
    /// Register all cron job handlers
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCronJobHandlers(this IServiceCollection services)
    {
        // Register each cron job type with its handler
        services.AddCronJob<CustomCronJob, CustomCronJobHandler>();
        services.AddCronJob<EmailCronJob, EmailCronJobHandler>();

        // Add more cron job types as needed:
        // services.AddCronJob<AnotherCronJob, AnotherCronJobHandler>();

        return services;
    }
}
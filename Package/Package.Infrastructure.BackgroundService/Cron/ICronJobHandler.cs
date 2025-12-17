namespace Package.Infrastructure.BackgroundServices.Cron;

/// <summary>
/// Interface for handling specific cron job execution logic
/// </summary>
/// <typeparam name="T">The cron job settings type</typeparam>
public interface ICronJobHandler<in T> where T : CronJobSettings
{
    /// <summary>
    /// Execute the specific job logic
    /// </summary>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <param name="cronJob">The cron job configuration</param>
    /// <param name="stoppingToken">Cancellation token</param>
    Task ExecuteAsync(string traceId, T cronJob, CancellationToken stoppingToken = default);
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.BackgroundServices.Work;

/// <summary>
/// Handles graceful completion of channel background task queue on application shutdown
/// </summary>
public class ChannelCompletionService(
    ChannelBackgroundTaskQueue taskQueue,
    IHostApplicationLifetime appLifetime,
    ILogger<ChannelCompletionService> logger) : IHostedService
{
    private readonly ChannelBackgroundTaskQueue _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    private readonly IHostApplicationLifetime _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
    private readonly ILogger<ChannelCompletionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Application is stopping. Completing the task queue.");
            _taskQueue.Complete();
            _logger.LogInformation("Task queue completed with {Count} remaining items", _taskQueue.Count);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes work items from a channel-based queue
/// using a non-polling, event-driven approach
/// </summary>
public class ChannelBackgroundTaskService(
    ChannelBackgroundTaskQueue taskQueue,
    ILogger<ChannelBackgroundTaskService> logger) : BackgroundService
{
    private readonly ChannelBackgroundTaskQueue _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    private readonly ILogger<ChannelBackgroundTaskService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeSpan _errorRetryDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Channel Background Task Service is starting");

        try
        {
            // Use the ReadAllAsync method to get work items as they become available
            await foreach (var workItem in _taskQueue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Executing background task");
                    await workItem(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected when shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing background task");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal cancellation, exit gracefully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during background task processing");

            try
            {
                // Add delay to avoid high CPU usage in case of persistent errors
                await Task.Delay(_errorRetryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Ignore
            }
        }

        _logger.LogInformation("Channel Background Task Service is stopping");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Channel Background Task Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
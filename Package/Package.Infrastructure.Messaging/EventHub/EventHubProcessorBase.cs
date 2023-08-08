using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Package.Infrastructure.Messaging.EventHub;

//https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventhubs.processor-readme?view=azure-dotnet

/// <summary>
/// 
/// </summary>
public abstract class EventHubProcessorBase : IEventHubProcessor, IDisposable
{
    private readonly ILogger<EventHubProcessorBase> _logger;
    private readonly EventHubProcessorSettingsBase _settings;
    private readonly EventProcessorClient _ehProcessorClient;

    protected EventHubProcessorBase(ILogger<EventHubProcessorBase> logger, IOptions<EventHubProcessorSettingsBase> settings,
        IAzureClientFactory<EventProcessorClient> clientFactory)

    {
        _logger = logger;
        _settings = settings.Value;
        _ehProcessorClient = clientFactory.CreateClient(_settings.EventHubProcessorClientName);
    }

    /// <summary>
    /// Register handler and start processing Event Hub events until cancellationToken.IsCancellationRequested
    /// </summary>
    /// <param name="funcProcess">should periocially (after n events processed) await args.UpdateCheckpointAsync();</param>
    /// <param name="funcError"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RegisterAndStartEventProcessor(Func<ProcessEventArgs, Task> funcProcess, Func<ProcessErrorEventArgs, Task> funcError, CancellationToken cancellationToken)
    {
        _logger.LogInformation("RegisterAndStartEventProcessor Start - {EventHubProcessorClientName}", _settings.EventHubProcessorClientName);

        _ehProcessorClient.ProcessEventAsync += funcProcess;
        _ehProcessorClient.ProcessErrorAsync += funcError;

        await StartProcessingAsync(cancellationToken); //background thread

        try
        {
            // The processor performs its work in the background; block until cancellation
            // to allow processing to take place.

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // This is expected when the delay is canceled.
        }

        try
        {
            await _ehProcessorClient.StopProcessingAsync(cancellationToken);
        }
        finally
        {
            // To prevent leaks, the handlers should be removed when processing is complete.
            _ehProcessorClient.ProcessEventAsync -= funcProcess;
            _ehProcessorClient.ProcessErrorAsync -= funcError;
        }

        _logger.LogInformation("RegisterAndStartEventProcessor Stopped - {EventHubProcessorClientName}", _settings.EventHubProcessorClientName);

    }

    /// <summary>
    /// Typically not needed; Use only to start a processor after manually stopping a processor with StopProcessingAsync
    /// Register starts processing already so this method is not needed after Register
    /// </summary>
    /// <param name="consumerGroup"></param>
    /// <param name="eventHubName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StartProcessingAsync - {EventHubProcessorClientName}", _settings.EventHubProcessorClientName);
        await _ehProcessorClient.StartProcessingAsync(cancellationToken); //background thread
    }

    /// <summary>
    /// Typically not needed; Manually stop processing but keeps the processor alive for subsequent StartProcessingAsync
    /// </summary>
    /// <param name="consumerGroup"></param>
    /// <param name="eventHubName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StopProcessingAsync - {EventHubProcessorClientName}", _settings.EventHubProcessorClientName);
        await _ehProcessorClient.StopProcessingAsync(cancellationToken);
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _ehProcessorClient.StopProcessing();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~EventHubProcessorBase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

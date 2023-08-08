using Azure.Messaging.EventHubs.Processor;

namespace Package.Infrastructure.Messaging.EventHub;
public interface IEventHubProcessor
{
    Task RegisterAndStartEventProcessor(Func<ProcessEventArgs, Task> funcProcess, Func<ProcessErrorEventArgs, Task> funcError, CancellationToken cancellationToken);
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task StopProcessingAsync(CancellationToken cancellationToken = default);

}

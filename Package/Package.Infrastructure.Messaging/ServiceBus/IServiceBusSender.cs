namespace Package.Infrastructure.Messaging.ServiceBus;

public interface IServiceBusSender
{
    Task SendMessageAsync(string queueOrTopicName, string message, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    Task SendBatchAsync<T>(string queueOrTopicName, ICollection<T> batch, CancellationToken cancellationToken = default);
}

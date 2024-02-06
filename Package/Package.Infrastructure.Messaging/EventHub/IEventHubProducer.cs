namespace Package.Infrastructure.Messaging.EventHub;
public interface IEventHubProducer
{
    Task SendAsync(string message, string? partitionId = null, string? partitionKey = null, string? correlationId = null, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    Task SendBatchAsync<T>(ICollection<T> batch, string? partitionId = null, string? partitionKey = null, string? correlationId = null, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
}

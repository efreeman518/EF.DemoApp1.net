namespace Package.Infrastructure.Messaging.EventGrid;

public interface IEventGridPublisher
{
    Task<int> SendAsync(EventGridEvent egEvent, CancellationToken cancellationToken = default);
}

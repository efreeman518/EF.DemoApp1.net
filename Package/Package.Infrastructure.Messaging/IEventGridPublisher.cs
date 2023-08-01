namespace Package.Infrastructure.Messaging;

public interface IEventGridPublisher
{
    Task<int> SendAsync(EventGridEvent egEvent, CancellationToken cancellationToken = default);
}

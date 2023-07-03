namespace Package.Infrastructure.Messaging;

public interface IEventGridPublisherManager
{
    Task<int> SendAsync(EventGridRequest request, CancellationToken cancellationToken = default);
}

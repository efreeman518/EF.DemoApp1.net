using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBus;

public interface IMessageHandler<in T> where T : IMessage
{
    Task HandleAsync(T message, CancellationToken cancellationToken = default);
}

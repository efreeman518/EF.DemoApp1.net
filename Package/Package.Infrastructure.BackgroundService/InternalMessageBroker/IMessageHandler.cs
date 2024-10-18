using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;

public interface IMessageHandler<in T> where T : IMessage
{
    Task HandleAsync(T message);
}

using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBus;
public interface IInternalMessageBus
{
    void AutoRegisterHandlers();

    void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void UnregisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void Publish<T>(InternalMessageBusProcessMode mode, ICollection<T> messages) where T : IMessage;
}

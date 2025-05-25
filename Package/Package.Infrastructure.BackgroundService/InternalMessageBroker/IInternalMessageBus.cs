using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;
public interface IInternalMessageBus
{
    void AutoRegisterHandlers();

    void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void UnregisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void Raise<T>(InternalMessageBusProcessMode mode, ICollection<T> messages) where T : IMessage;
}

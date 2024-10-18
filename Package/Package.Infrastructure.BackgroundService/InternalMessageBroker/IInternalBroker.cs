using Package.Infrastructure.Common.Contracts;

namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;
public interface IInternalBroker
{
    void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void RaiseRegistered<T>(InternalBrokerProcessMode mode, ICollection<T> messages) where T : IMessage;

    void Raise<T>(InternalBrokerProcessMode mode, ICollection<T> messages) where T : IMessage;
}

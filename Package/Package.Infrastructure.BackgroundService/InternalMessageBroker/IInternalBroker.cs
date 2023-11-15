namespace Package.Infrastructure.BackgroundServices.InternalMessageBroker;
public interface IInternalBroker
{
    void RegisterMessageHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    void ProcessRegistered<T>(ProcessInternalMode mode, ICollection<T> messages) where T : IMessage;

    void Process<T>(ProcessInternalMode mode, ICollection<T> messages) where T : IMessage;
}

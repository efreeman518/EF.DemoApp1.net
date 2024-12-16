using OpenAI.Chat;

namespace Package.Infrastructure.AzureOpenAI.Chat;

public partial class Chat(List<ChatMessage>? messages = null)
{
    private readonly Guid _id = Guid.CreateVersion7();

    private readonly List<ChatMessage> _messages = messages ?? [];

    public Guid Id
    {
        get { return _id; }
        init { if (value != Guid.Empty) _id = value; }
    }

    public List<ChatMessage> Messages => _messages;

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
    }
}

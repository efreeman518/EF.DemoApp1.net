using OpenAI.Chat;

namespace Package.Infrastructure.AzureOpenAI;

public interface IChatService
{
    Task<List<string>> ChatStream(Request request);
    Task<string> ChatCompletion(Request request);
    Task<string> ChatCompletionWithTools(Request request);
    Task ChatCompletionWithTools(List<ChatMessage> messages, ChatCompletionOptions? options = null,
        Func<List<ChatMessage>, IReadOnlyList<ChatToolCall>, Task>? toolCallFunc = null);
}

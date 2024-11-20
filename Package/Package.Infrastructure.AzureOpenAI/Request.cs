using OpenAI.Chat;

namespace Package.Infrastructure.AzureOpenAI;

public class Request(string prompt)
{
    public string Prompt => prompt;
    public IList<ChatTool>? Tools { get; set; }
    public Func<List<ChatMessage>, IReadOnlyList<ChatToolCall>>? ToolCalls { get; set; }
}

namespace Package.Infrastructure.OpenAI.ChatApi;

public interface IChatService
{
    Task<List<string>> ChatStream(Request request);
    Task<string> ChatCompletion(Request request);
    Task<string> ChatCompletionWithTools(Request request);
}

namespace Package.Infrastructure.OpenAI.ChatApi;

public class Request
{
    private readonly string _prompt;
    public string Prompt => _prompt;

    public Request(string prompt)
    {
        _prompt = prompt;
    }
}

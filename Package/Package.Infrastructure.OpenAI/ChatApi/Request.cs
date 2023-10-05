namespace Package.Infrastructure.OpenAI.ChatApi;

public class Request(string prompt)
{
    public string Prompt => prompt;
}

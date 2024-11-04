namespace Package.Infrastructure.AzureOpenAI.ChatApi;

public class Request(string prompt)
{
    public string Prompt => prompt;
}

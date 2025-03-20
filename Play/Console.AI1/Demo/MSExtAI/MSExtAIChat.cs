using Azure.AI.OpenAI;
using Console.AI1.Demo.MSExtAI.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Console.AI1.Demo.MSExtAI;

//https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions
//https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/use-function-calling?tabs=azd&pivots=azure-openai

internal class MSExtAIChat(IConfigurationRoot config, IServiceCollection services, AzureOpenAIClient aoaiClient)
{
    public async Task RunAsync()
    {
        var aoaiConfig = config.GetSection("AzureOpenAI");
        var chatDeployment = aoaiConfig.GetValue<string>("DefaultChatDeployment")!;
        IChatClient chatClientInner = aoaiClient.AsChatClient(chatDeployment);
        IChatClient chatClient = new ChatClientBuilder(chatClientInner)
            .UseFunctionInvocation()
            .Build();

        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() =>
                {
                    return new DataPluginA(config).SearchItems();
                },
                "DatabaseASearchItems",
                "Retrieves items from DatabaseA, returns a list of items."),
                AIFunctionFactory.Create(() =>
                {
                    return new DataPluginB(config).SearchItems();
                },
                "DatabaseBSearchItems",
                "Retrieves items from DatabaseB, returns a list of items."),
                AIFunctionFactory.Create((List<ChatMessage> chatHistory) => async () =>
                {
                    var chatJoined = string.Join("#\n", chatHistory.Select(c => $"{c.Role} > {c.Text}"));
                    var summary = await chatClient.GetResponseAsync([
                        new(ChatRole.System, $"Summarize the following conversation, maintianing the initial system prompt and important information:###\n{chatJoined}"),
                    ]);
                    return summary.Message;
                },
                "SummarizeChat",
                "Returns a summarization of the chat, maintianing the system prompt and important information."),
            ]
        };

        List<ChatMessage> chatHistory = CreateChatHistory();

        while (true)
        {  
            ChatResponse response = await chatClient.GetResponseAsync(chatHistory, chatOptions);
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response.Message.Contents));
            System.Console.WriteLine($"{chatHistory[^1].Role} >>> {chatHistory[^1]}");

            if (chatHistory.Count > 10)
            {
                var summary = await SummarizeChatHistory(chatClient, chatHistory);
                chatHistory.Clear();
                chatHistory.Add(new ChatMessage(ChatRole.System, summary.Contents));
                System.Console.WriteLine($"Summarized: {chatHistory[^1].Role} >>> {chatHistory[^1]}");
            }

            var input = System.Console.ReadLine();
            chatHistory.Add(new ChatMessage(ChatRole.User, input));
        }
    }

    private async Task< ChatMessage> SummarizeChatHistory(IChatClient chatClient, List<ChatMessage> chatHistory)
    {
        var chatJoined = string.Join("#\n", chatHistory.Select(c => $"{c.Role} > {c.Text}"));
        var summary = await chatClient.GetResponseAsync([
            new(ChatRole.System, $"Summarize the following conversation, maintianing the initial system prompt and important information:###\n{chatJoined}"),
        ]);
        return summary.Message;
    }

    private List<ChatMessage> CreateChatHistory(List<ChatMessage>? init = null)
    {
        List<ChatMessage> chatHistory = [new(ChatRole.System, """
            Your job is to find the best matching items from DatabaseA and DatabaseB without asking for any additional information.
            """)];
        if (init != null)
        {
            chatHistory.AddRange(init);
        }
        return chatHistory;
    }
}

using Azure.AI.OpenAI;
using Console.AI1.Demo.SKMultiAgentChat.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

//https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/examples/example-chat-agent?pivots=programming-language-csharp

namespace Console.AI1.Demo.SKMultiAgentChat;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal class MultiAgentChat(IConfigurationRoot config, IServiceCollection services, AzureOpenAIClient aoaiClient)
{
    public async Task RunAsync()
    {
        var aoaiConfig = config.GetSection("AzureOpenAI");
        var chatDeployment = aoaiConfig.GetValue<string>("DefaultChatDeployment")!;
        //services.AddAzureOpenAIChatCompletion(chatDeployment, aoaiClient);

        IKernelBuilder kernelBuilderA = Kernel.CreateBuilder();
        kernelBuilderA.AddAzureOpenAIChatCompletion(chatDeployment, aoaiClient);
        //kernelBuilderA.AddOpenAIChatCompletion("phi4-mini", new Uri("http://localhost:11434/v1"), "ollama"); //"phi4-mini" "qwen2.5:1.5b" "mistral"
        kernelBuilderA.Plugins.AddFromObject(new AgentAItemsPlugin(config), "ItemsDatabaseA");
        var kernelA = kernelBuilderA.Build();

        IKernelBuilder kernelBuilderB = Kernel.CreateBuilder();
        kernelBuilderB.AddAzureOpenAIChatCompletion(chatDeployment, aoaiClient);
        //kernelBuilderB.AddOpenAIChatCompletion("phi4-mini", new Uri("http://localhost:11434/v1"), "ollama");
        kernelBuilderB.Plugins.AddFromObject(new AgentBItemsPlugin(config), "ItemsDatabaseB");
        var kernelB = kernelBuilderB.Build();


        //foreach (var plugin in kernelA.Plugins)
        //{
        //    System.Console.WriteLine($"Plugin: {plugin.Name}");
        //    foreach (var function in plugin.GetFunctionsMetadata())
        //    {
        //        System.Console.WriteLine($"  Function: {function.Name}");
        //    }
        //}
        //var result = await kernelA.InvokeAsync("ItemsDatabaseA", "SearchItems");
        //foreach (var item in result.GetValue<List<Item>>()!)
        //{
        //    System.Console.WriteLine($"Item: {item.SerializeToJson()}");
        //}


        /*
         * You can request the data (with optional criteria) from other agents, present the structure of your own data, or request the structure of data from other agents.
         * You can give an example of your data.
         */
        ChatCompletionAgent agentA = new()
        {
            Name = "AgentA",
            //Instructions = "Your task is to provide your items upon requst. You can only get items from your items database.",
            Instructions = @"Your task is to match your items with another agent's items, 
find the closest match based on the property EnumList, 
even if they are not exact matches. 
You can only search items from your items database and find matches from AgentB's items database.
When you determine the best match, show both your object and the other agent's object noting why it's the best match, any differences, and end the conversation with 'TERMINATE",
            Kernel = kernelA,
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        ChatCompletionAgent agentB = new()
        {
            Name = "AgentB",
            Instructions = "Your task is to provide your database items when AgentA asks for them. You can only get items from your items database.",
            Kernel = kernelB,
            Arguments =
            new KernelArguments(new AzureOpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        AgentGroupChat groupChat = new(agentB, agentA)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new CustomTerminationStrategy()
            }
        };

        string startConvo = "AgentA find matching data items from your database and AgentB's database";
        //string startConvo = "AgentA whats in your database?";
        //var chatHistory = new ChatHistory();
        //chatHistory.Add(new ChatMessageContent(AuthorRole.User, startConvo));
        //var response = agentA.InvokeAsync(chatHistory);
        //await foreach(var content in response)
        //{
        //    System.Console.WriteLine($"# {AuthorRole.User} - {content.AuthorName ?? "*"}: '{content.Content}'");
        //}

        groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, startConvo));

        System.Console.WriteLine($"# {AuthorRole.User}: '{startConvo}'");

        await foreach (ChatMessageContent content in groupChat.InvokeAsync())
        {
            System.Console.WriteLine($"# {AuthorRole.User} - {content.AuthorName ?? "*"}: '{content.Content}'");
        }

        System.Console.WriteLine($"# IS COMPLETE: {groupChat.IsComplete}");


    }
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


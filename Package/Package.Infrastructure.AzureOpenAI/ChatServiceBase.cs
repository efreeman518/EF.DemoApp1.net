using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.AzureOpenAI;

//https://github.com/openai/openai-dotnet?tab=readme-ov-file
//https://platform.openai.com/settings/organization/usage
//https://platform.openai.com/docs/models

/*
 * AzureOpenAI - when you access the model via the API, you need to refer to the deployment name rather than the underlying model name in API calls, 
 * which is one of the key differences between OpenAI and Azure OpenAI. OpenAI only requires the model name. 
 * Azure OpenAI always requires deployment name, even when using the model parameter. 
 * In our docs, we often have examples where deployment names are represented as identical to model names 
 * to help indicate which model works with a particular API endpoint. Ultimately your deployment names can 
 * follow whatever naming convention is best for your use case.
*/

public abstract class ChatServiceBase (ILogger<ChatServiceBase> logger, IOptions<ChatServiceSettingsBase> settings, AzureOpenAIClient openAIclient, IFusionCacheProvider cacheProvider) : IChatService
{
    private readonly ChatClient chatClient = openAIclient.GetChatClient(settings.Value.DeploymentName);
    private readonly IFusionCache cache = cacheProvider.GetCache(settings.Value.CacheName);

    //private readonly AzureOpenAIClient openAIclient = new(new Uri(settings.Value.Url), new DefaultAzureCredential());

    public async Task<string> ChatCompletionAsync(Guid? chatId, List<ChatMessage> newMessages, ChatCompletionOptions? options = null,
        Func<List<ChatMessage>, IReadOnlyList<ChatToolCall>, Task>? toolCallFunc = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("ChatCompletionAsync - {ChatId}", chatId);

        bool requiresAction;

        var cacheKey = $"chat-{chatId}";

        //get the chat from the cache
        Chat chat;
        if (chatId != null)
        {
            var chatCache = await cache.TryGetAsync<Chat>(cacheKey, token: cancellationToken);
            chat = chatCache.HasValue ? chatCache.Value : new Chat();
        }
        else
        {
            chat = new Chat();
        }

        //add the message to the chat
        foreach (var message in newMessages)
        {
            chat.AddMessage(message);
        }

        do
        {
            requiresAction = false;
            ChatCompletion completion = await chatClient.CompleteChatAsync(chat.Messages, options, cancellationToken);

            switch (completion.FinishReason)
            {
                //model has determined the conversation is complete
                case ChatFinishReason.Stop:
                    {
                        // Add the assistant message to the conversation history.
                        chat.Messages.Add(new AssistantChatMessage(completion));
                        break;
                    }
                //model has requested additional information
                case ChatFinishReason.ToolCalls:
                    {
                        ArgumentNullException.ThrowIfNull(toolCallFunc);
                        // First, add the assistant message with tool calls to the conversation history.
                        chat.Messages.Add(new AssistantChatMessage(completion));

                        //process the tool calls; adds tool response messages to the chat 
                        await toolCallFunc(chat.Messages, completion.ToolCalls);

                        //tools have been called and responses added to messages; more work is required to complete the conversation
                        requiresAction = true;

                        break;
                    }

                case ChatFinishReason.Length:
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                case ChatFinishReason.ContentFilter:
                    throw new NotImplementedException("Omitted content due to a content filter flag.");

                case ChatFinishReason.FunctionCall:
                    throw new NotImplementedException("Deprecated in favor of tool calls.");

                default:
                    throw new NotImplementedException(completion.FinishReason.ToString());
            }
        } while (requiresAction);

        //save the chat to the cache
        await cache.SetAsync(cacheKey, chat, token: cancellationToken);

        return chat.Messages[^1].Content.ToString()!; 
    }

    public async Task<string> ChatCompletionWithDataSource(Request request)
    {
        ChatCompletionOptions options = new();

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        options.AddDataSource(new AzureSearchChatDataSource()
        {
            Endpoint = new Uri("https://your-search-resource.search.windows.net"),
            IndexName = "contoso-products-index",
            Authentication = DataSourceAuthentication.FromApiKey(
                Environment.GetEnvironmentVariable("OYD_SEARCH_KEY")),
        });

        //running list of messages to be sent to the model with each request
        List<ChatMessage> messages =
        [
            new UserChatMessage(request.Prompt) //("What's the details of some static data?"),
        ];

        StringBuilder response = new();

        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);
        ChatMessageContext dataContext = completion.GetMessageContext();

        if (dataContext?.Intent is not null)
        {
            response.Append($"Intent: {dataContext.Intent}");
        }
        foreach (ChatCitation citation in dataContext?.Citations ?? [])
        {
            response.Append($"{Environment.NewLine}Citation: {citation.Content}");
        }

        return response.ToString();

#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}


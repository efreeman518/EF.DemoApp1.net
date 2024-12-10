using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Package.Infrastructure.Common.Extensions;
using System.Text;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.AzureOpenAI;

//https://github.com/openai/openai-dotnet?tab=readme-ov-file
//https://platform.openai.com/settings/organization/usage
//https://platform.openai.com/docs/models

/*
 * AzureOpenAI - when you access the model via the API, you need to refer to the deployment name rather than the underlying model name in API calls, 
 * which is one of the key differences between OpenAI and Azure OpenAI. OpenAI only requires the model name. 
 * Azure OpenAI always requires deployment name, even when using the model parameter. 
*/

public abstract class ChatServiceBase(ILogger<ChatServiceBase> logger, IOptions<ChatServiceSettingsBase> settings,
    AzureOpenAIClient openAIclient, IFusionCacheProvider cacheProvider) : IChatService
{
    private readonly ChatClient chatClient = openAIclient.GetChatClient(settings.Value.DeploymentName);
    private readonly IFusionCache cache = cacheProvider.GetCache(settings.Value.CacheName);

    private static readonly JsonSerializerOptions optSer = new()
    {
        Converters = { new ChatMessageJsonConverter() }
    };

    /// <summary>
    /// ChatCompletionAsync - Completes a chat conversation with the OpenAI model.
    /// </summary>
    /// <param name="chatId">Track/cache</param>
    /// <param name="newMessages">incoming</param>
    /// <param name="options">define available tools for the chat</param>
    /// <param name="toolCallFunc">callback to run the tool methods requested by the model</param>
    /// <param name="maxCompletionMessageCount">limit msgs sent into the model to the most recent</param>
    /// <param name="maxToolCallRounds">the model can get stuck in a loop repeatedly calling tools</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<(Guid, string)> ChatCompletionAsync(Guid? chatId, List<ChatMessage> newMessages, ChatCompletionOptions? options = null,
        Func<List<ChatMessage>, IReadOnlyList<ChatToolCall>, Task>? toolCallFunc = null, int? maxCompletionMessageCount = null, int maxToolCallRounds = 5,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("ChatCompletionAsync - {ChatId}", chatId);

        bool requiresAction;

        var cacheKey = $"chat-{chatId}";

        //get the chat from the cache
        Chat chat;
        if (chatId != null)
        {
            //may have expired, in that case restart with a new chat
            var chatjson = (await cache.GetOrDefaultAsync<string>(cacheKey, token: cancellationToken));
            if (chatjson != null)
            {
                chat = chatjson.DeserializeJson<Chat>(optSer)!;
            }
            else
            {
                chat = new Chat();
                cacheKey = $"chat-{chat.Id}";
            }
        }
        else
        {
            chat = new Chat();
            cacheKey = $"chat-{chat.Id}";
        }

        //add new messages to the chat
        foreach (var message in newMessages)
        {
            chat.AddMessage(message);
        }

        var chatToolRounds = 0;
        do
        {
            //sometimes the model gets stuck repeatedly calling tools; limit the number of rounds
            if (chatToolRounds++ > maxToolCallRounds)
            {
                throw new InvalidOperationException("Exceeded maximum number of contiguous tool call rounds.");
            }
            requiresAction = false;

            List<ChatMessage> msgs = chat.Messages;

            //determine some logic to limit the number of messages sent to the model
            //if (maxCompletionMessageCount != null && maxCompletionMessageCount > 0 && maxCompletionMessageCount < chat.Messages.Count)
            //{
            //    msgs = GetRecentChatMessages(chat.Messages, maxCompletionMessageCount.Value);
            //}
            //else
            //{
            //    msgs = chat.Messages;
            //}

            //seems to get a 401 when hitting a token limit (gpt-4o-mini)
            ChatCompletion completion = await chatClient.CompleteChatAsync(msgs, options, cancellationToken);

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
        await cache.SetAsync(cacheKey, chat.SerializeToJson(optSer), token: cancellationToken);

        return (chat.Id, chat.Messages[^1].Content[0].Text);
    }

    /// <summary>
    /// GetRecentChatMessages - Get the most recent chat messages to conserve tokens
    /// Defend against invalid messages list going in to the model, like cutting off a tool-call message prior to a tool message
    /// </summary>
    /// <param name="msgs"></param>
    /// <param name="maxMsgCount"></param>
    /// <returns></returns>
    //private static List<ChatMessage> GetRecentChatMessages(List<ChatMessage> msgs, int maxMsgCount)
    //{
    //    if (msgs.Count <= maxMsgCount)
    //    {
    //        return msgs;
    //    }

    //    var msgsRecent = new List<ChatMessage>(); 
    //    msgsRecent.AddRange(msgs);

    //    //remove at index 1 until the count is less than the max
    //    for(int i = msgs.Count - 1; i > maxMsgCount; i--)
    //    {
    //        msgsRecent.RemoveAt(1);
    //    }

    //    //remove messages from index 1 if they are referring to tools
    //    while(msgsRecent.Count > 1 && (msgsRecent[1] is ToolChatMessage || (msgsRecent[1] is AssistantChatMessage assistantMessage && assistantMessage.ToolCalls != null)))
    //    {
    //        msgsRecent.RemoveAt(1);
    //    }

    //    return msgsRecent;
    //}

    public async Task<string> ChatCompletionWithDataSource(Request request)
    {
        ChatCompletionOptions options = new();

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable S1075 // URIs should not be hardcoded
        options.AddDataSource(new AzureSearchChatDataSource()
        {
            Endpoint = new Uri("https://your-search-resource.search.windows.net"),
            IndexName = "contoso-products-index",
            Authentication = DataSourceAuthentication.FromApiKey(
                Environment.GetEnvironmentVariable("OYD_SEARCH_KEY")),
        });
#pragma warning restore S1075 // URIs should not be hardcoded

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


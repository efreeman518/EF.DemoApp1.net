using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Package.Infrastructure.OpenAI.ChatApi;

//https://github.com/openai/openai-dotnet?tab=readme-ov-file
//https://platform.openai.com/settings/organization/usage
//https://platform.openai.com/docs/models

public class ChatService : IChatService
{
    private readonly ChatClient chatClient;

    public ChatService(IOptions<ChatServiceSettings> settings)
    {
        var openAIclient = new OpenAIClient(settings.Value.Key);
        chatClient = openAIclient.GetChatClient(settings.Value.Model);
    }

    public async Task<List<string>> ChatStream(Request request)
    {
        List<string> response = [];

        AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(request.Prompt);

        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                response.Add(completionUpdate.ContentUpdate[0].Text);
            }
        }

        return response;
    }

    public async Task<string> ChatCompletion(Request request)
    {
        ChatCompletion completion = await chatClient.CompleteChatAsync(request.Prompt);
        return completion.Content[0].Text;
    }
}


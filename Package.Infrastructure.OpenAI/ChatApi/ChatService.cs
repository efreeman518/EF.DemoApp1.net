using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace Package.Infrastructure.OpenAI.ChatApi;

//https://github.com/OkGoDoIt/OpenAI-API-dotnet

public class ChatService : IChatService
{
    private readonly OpenAIAPI _openApi;
    public ChatService(IOptions<ChatServiceSettings> settings)
    {
        _openApi = new OpenAIAPI(settings.Value.Key);
    }

    public async Task<List<string>> ChatStream(Request request)
    {
        var chat = _openApi.Chat.CreateConversation(new ChatRequest { Model= "gpt-3.5-turbo" });
        chat.AppendUserInput(request.Prompt);

        List<string> response = new();
        await foreach (var res in chat.StreamResponseEnumerableFromChatbotAsync())
        {
            response.Add(res);
        }
        return response;
    }

    public async Task<string> ChatCompletion(Request request)
    {
        var result = await _openApi.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.5,
            MaxTokens = 50,
            Messages = new ChatMessage[] {
            new ChatMessage(ChatMessageRole.User, request.Prompt)
        }
        });
        // or
        //var result = api.Chat.CreateChatCompletionAsync("Hello!");

        return result.ToString();
    }
}


using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using Package.Infrastructure.AzureOpenAI.Chat;
using Package.Infrastructure.Test.Integration.AzureOpenAI.Chat;

namespace Package.Infrastructure.Test.Integration;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class AzureOpenAIChatServiceTests : IntegrationTestBase
{
    readonly IChatService _chatService;

    public AzureOpenAIChatServiceTests()
    {
        _chatService = Services.GetRequiredService<ISomeChatService>();
    }

    [TestMethod]
    public async Task Conversation_pass()
    {
        var msgs = new List<ChatMessage> {
            SystemChatMessage.CreateSystemMessage("You are an expert in everything and directly answer questions from the user."),
            UserChatMessage.CreateUserMessage("Why is the sky blue")
        };

        (var chatId, var chatResponse) = await _chatService.ChatCompletionAsync(null, msgs);
        Assert.IsNotNull(chatId);
        Assert.IsNotNull(chatResponse);
    }
}

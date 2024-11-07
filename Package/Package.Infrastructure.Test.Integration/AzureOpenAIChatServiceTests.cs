using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.AzureOpenAI.ChatApi;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class AzureOpenAIChatServiceTests : IntegrationTestBase
{
    readonly IChatService _chatService;

    public AzureOpenAIChatServiceTests()
    {
        _chatService = Services.GetRequiredService<IChatService>();
    }

    [TestMethod]
    public async Task Conversation_pass()
    {
        var request = new Request("Why is the sky blue?");
        var responseList = await _chatService.ChatStream(request);
        Assert.IsNotNull(responseList);

        var response = await _chatService.ChatCompletion(request);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    [DataRow("Whats the weather like today in Omaha, celsius?")]
    [DataRow("What is the weather like today?")]
    public async Task ConversationWithTools_pass(string prompt)
    {
        var request = new Request(prompt);
        var response = await _chatService.ChatCompletionWithTools(request);
        Assert.IsNotNull(response);
    }
}

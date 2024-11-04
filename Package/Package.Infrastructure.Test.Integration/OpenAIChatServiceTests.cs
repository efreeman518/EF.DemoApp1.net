using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.OpenAI.ChatApi;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("OpenAI Api Key required - https://platform.openai.com/settings/organization/api-keys")]

[TestClass]
public class OpenAIChatServiceTests : IntegrationTestBase
{
    readonly IChatService _chatService;

    public OpenAIChatServiceTests()
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
    public async Task ConversationWithTools_pass()
    {
        var request = new Request("Whats the weather like today?");
        var response = await _chatService.ChatCompletionWithTools(request);
        Assert.IsNotNull(response);
    }
}

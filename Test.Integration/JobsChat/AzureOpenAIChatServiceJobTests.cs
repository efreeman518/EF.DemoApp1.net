using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Support;

namespace Test.Integration.JobsChat;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class AzureOpenAIChatServiceJobTests : IntegrationTestBase
{
    private readonly IJobChatOrchestrator _jobChat;

    public AzureOpenAIChatServiceJobTests()
    {
        ConfigureServices("AzureOpenAIChatServiceJobTests");
        _jobChat = ServiceScope.ServiceProvider.GetRequiredService<IJobChatOrchestrator>();
    }

    [TestMethod]
    public async Task JobSearchChat_pass()
    {
        var request = new ChatRequest { ChatId = null, Message = "memphis, 20 miles, er" };
        //retrieve response chat id & message
        ChatResponse? response = null;
        var result = await _jobChat.ChatCompletionAsync(request);
        _ = result.Match(
            dto => response = dto,
            err => throw err
            );

        Assert.IsNotNull(response?.ChatId != Guid.Empty);
        Assert.IsNotNull(response?.Message);

    }
}

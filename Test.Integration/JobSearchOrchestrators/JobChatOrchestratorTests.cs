using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Support;

namespace Test.Integration.JobSearchOrchestrators;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class JobChatOrchestratorTests : IntegrationTestBase
{
    private readonly IJobChatOrchestrator _jobChat;

    public JobChatOrchestratorTests()
    {
        ConfigureServices("JobChatOrchestratorTests");
        _jobChat = ServiceScope.ServiceProvider.GetRequiredService<IJobChatOrchestrator>();
    }

    [TestMethod]
    public async Task JobSearchChat_pass()
    {
        var request = new ChatRequest { ChatId = null, Message = "hi" };

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

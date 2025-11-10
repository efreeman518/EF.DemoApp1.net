using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
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

        var result = await _jobChat.ChatCompletionAsync(request);

        if (result.IsSuccess)
        {
            var response = result.Value;
            Assert.IsNotNull(response?.ChatId);
            Assert.AreNotEqual(Guid.Empty, response?.ChatId);
            Assert.IsNotNull(response?.Message);
        }
        else
        {
            throw new InvalidOperationException(string.Join(",", result.Errors));
        }
    }
}

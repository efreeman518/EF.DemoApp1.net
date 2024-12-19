using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Support;

namespace Test.Integration.JobSearchOrchestrators;

//[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class JobAssistantOrchestratorTests : IntegrationTestBase
{
    private readonly IJobAssistantOrchestrator _jobAssistant;

    public JobAssistantOrchestratorTests()
    {
        ConfigureServices("JobAssistantOrchestratorTests");
        _jobAssistant = ServiceScope.ServiceProvider.GetRequiredService<IJobAssistantOrchestrator>();
    }

    [TestMethod]
    public async Task JobSearchAssistant_pass()
    {
        var request = new AssistantRequest { Message = "hi" };

        AssistantResponse? response = null;
        var result = await _jobAssistant.AssistantRunAsync(request);
        _ = result.Match(
            dto => response = dto,
            err => throw err
            );

        Assert.IsNotNull(response?.AssistantId);
        Assert.IsNotNull(response?.ThreadId);
        Assert.IsNotNull(response?.Message);
    }
}

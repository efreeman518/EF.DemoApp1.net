using Application.Contracts.Model;
using Application.Services.JobSK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Support;

namespace Test.Integration.JobSearchOrchestrators;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

[TestClass]
public class JobSearchOrchestratorTests : IntegrationTestBase
{
    private readonly IJobSearchOrchestrator _jobSearch;

    public JobSearchOrchestratorTests()
    {
        ConfigureServices("JobSearchOrchestratorTests");
        _jobSearch = ServiceScope.ServiceProvider.GetRequiredService<IJobSearchOrchestrator>();
    }

    [TestMethod]
    public async Task JobSearchChat_pass()
    {
        var request = new ChatRequest { ChatId = null, Message = @"hi, I am looking for a nursing job in San Diego. I can work in ER and ICU. 
            I am not available on Fridays but I can work some nights and weekends. Please tell me what matching jobs you have available." };

        ChatResponse? response = null;
        var result = await _jobSearch.ChatCompletionAsync(request);
        _ = result.Match(
            dto => response = dto,
            err => throw err
            );

        Assert.IsNotNull(response?.ChatId != Guid.Empty);
        Assert.IsNotNull(response?.Message);
    }
}

using Azure.AI.OpenAI;
using Package.Infrastructure.AzureOpenAI;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services.JobChat;

public class JobChatService(ILogger<JobChatService> logger, IOptions<JobChatSettings> settings,
    AzureOpenAIClient openAIclient, IFusionCacheProvider cacheProvider) : ChatServiceBase(logger, settings, openAIclient, cacheProvider), IJobChatService
{
}

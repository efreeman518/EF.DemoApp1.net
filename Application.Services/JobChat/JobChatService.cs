using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Package.Infrastructure.AzureOpenAI.Chat;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services.JobChat;

public class JobChatService(ILogger<JobChatService> logger, IOptions<JobChatSettings> settings,
    IAzureClientFactory<AzureOpenAIClient> clientFactory, IFusionCacheProvider cacheProvider) : ChatServiceBase(logger, settings, clientFactory, cacheProvider), IJobChatService
{
}

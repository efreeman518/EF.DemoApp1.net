using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.Test.Integration.AzureAIChat;

public class JobChatService(ILogger<ChatServiceBase> logger, IOptions<ChatServiceSettingsBase> settings,
    AzureOpenAIClient openAIclient, IFusionCacheProvider cacheProvider) : ChatServiceBase(logger, settings, openAIclient, cacheProvider), IJobChatService
{
}

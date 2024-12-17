using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI.Chat;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Chat;

public class SomeChatService(ILogger<ChatServiceBase> logger, IOptions<SomeChatSettings> settings,
    IAzureClientFactory<AzureOpenAIClient> clientFactory, IFusionCacheProvider cacheProvider) : ChatServiceBase(logger, settings, clientFactory, cacheProvider), ISomeChatService
{
}

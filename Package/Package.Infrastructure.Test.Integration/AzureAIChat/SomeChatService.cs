using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI.Chat;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.Test.Integration.AzureAIChat;

public class SomeChatService(ILogger<ChatServiceBase> logger, IOptions<SomeChatSettings> settings,
    AzureOpenAIClient openAIclient, IFusionCacheProvider cacheProvider) : ChatServiceBase(logger, settings, openAIclient, cacheProvider), ISomeChatService
{
}

using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI.Assistant;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;

public class SomeAssistantService(ILogger<AssistantServiceBase> logger, IOptions<SomeAssistantSettings> settings, IAzureClientFactory<AzureOpenAIClient> clientFactory) : 
    AssistantServiceBase(logger, settings, clientFactory), ISomeAssistantService
{
}

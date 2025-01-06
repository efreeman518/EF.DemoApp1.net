using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI.Assistants;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;

public class SomeAssistantService(ILogger<AssistantServiceBase> logger, IOptions<SomeAssistantSettings> settings, AzureOpenAIClient aoaiClient) :
    AssistantServiceBase(logger, settings, aoaiClient), ISomeAssistantService
{
}

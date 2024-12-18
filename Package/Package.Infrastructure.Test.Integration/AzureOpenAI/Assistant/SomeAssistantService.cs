using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.AzureOpenAI.Assistant;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;

public class SomeAssistantService(ILogger<AssistantServiceBase> logger, IOptions<SomeAssistantSettings> settings, AssistantsClient client) : 
    AssistantServiceBase(logger, settings, client), ISomeAssistantService
{
}

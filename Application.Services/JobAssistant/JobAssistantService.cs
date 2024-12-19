using Azure.AI.OpenAI.Assistants;
using Package.Infrastructure.AzureOpenAI.Assistant;

namespace Application.Services.JobAssistant;

public class JobAssistantService(ILogger<JobAssistantService> logger, IOptions<JobAssistantSettings> settings, AssistantsClient client)
    : AssistantServiceBase(logger, settings, client), IJobAssistantService
{
}

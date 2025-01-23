using Azure.AI.OpenAI;
using Package.Infrastructure.AzureOpenAI.Assistants;

namespace Application.Services.JobAssistant;

public class JobAssistantService(ILogger<JobAssistantService> logger, IOptions<JobAssistantServiceSettings> settings, AzureOpenAIClient aoaiClient)
    : AssistantServiceBase(logger, settings, aoaiClient), IJobAssistantService
{
}

using Package.Infrastructure.AzureOpenAI.Assistants;

namespace Application.Services.JobAssistant;
public class JobAssistantServiceSettings : AssistantServiceSettingsBase
{
    public static string ConfigSectionName => "JobAssistantServiceSettings";
}

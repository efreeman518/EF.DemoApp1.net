using Package.Infrastructure.AzureOpenAI.Assistants;

namespace Application.Services.JobAssistant;
public class JobAssistantSettings : AssistantServiceSettingsBase
{
    public static string ConfigSectionName => "JobAssistantSettings";
}

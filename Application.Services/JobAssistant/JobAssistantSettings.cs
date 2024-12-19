using Package.Infrastructure.AzureOpenAI.Assistant;

namespace Application.Services.JobAssistant;
public class JobAssistantSettings : AssistantServiceSettingsBase
{
    public static string ConfigSectionName => "JobAssistantSettings";
}

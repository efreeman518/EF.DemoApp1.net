using Package.Infrastructure.AzureOpenAI;

namespace Application.Services.JobChat;
public class JobChatSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "JobChatSettings";
}

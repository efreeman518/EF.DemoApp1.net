using Package.Infrastructure.AzureOpenAI.Chat;

namespace Application.Services.JobChat;
public class JobChatServiceSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "JobChatServiceSettings";
}

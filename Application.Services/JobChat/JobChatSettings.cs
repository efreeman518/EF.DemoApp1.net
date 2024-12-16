using Package.Infrastructure.AzureOpenAI.Chat;

namespace Application.Services.JobChat;
public class JobChatSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "JobChatSettings";
}

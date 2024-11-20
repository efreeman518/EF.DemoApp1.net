using Package.Infrastructure.AzureOpenAI;

namespace Package.Infrastructure.Test.Integration.AzureAIChat;
public class JobChatSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "JobChatSettings";
}

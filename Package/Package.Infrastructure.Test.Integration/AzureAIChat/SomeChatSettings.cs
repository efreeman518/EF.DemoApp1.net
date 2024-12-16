using Package.Infrastructure.AzureOpenAI.Chat;

namespace Package.Infrastructure.Test.Integration.AzureAIChat;
public class SomeChatSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "SomeChatSettings";
}

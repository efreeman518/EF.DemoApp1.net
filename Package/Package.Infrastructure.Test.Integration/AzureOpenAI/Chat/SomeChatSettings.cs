using Package.Infrastructure.AzureOpenAI.Chat;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Chat;

public class SomeChatSettings : ChatServiceSettingsBase
{
    public static string ConfigSectionName => "SomeChatSettings";
}

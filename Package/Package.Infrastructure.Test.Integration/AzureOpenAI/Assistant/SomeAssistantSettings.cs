using Package.Infrastructure.AzureOpenAI.Assistants;

namespace Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;

public class SomeAssistantSettings : AssistantServiceSettingsBase
{
    public static string ConfigSectionName => "SomeAssistantSettings";
}

namespace Package.Infrastructure.AzureOpenAI.Chat;

public abstract class ChatServiceSettingsBase
{
    public string DeploymentName { get; set; } = null!;
    public string CacheName { get; set; } = null!;
}

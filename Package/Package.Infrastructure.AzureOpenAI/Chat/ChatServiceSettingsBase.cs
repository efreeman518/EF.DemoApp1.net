namespace Package.Infrastructure.AzureOpenAI.Chat;

public abstract class ChatServiceSettingsBase
{
    public string ResourceName { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public string CacheName { get; set; } = null!;
}

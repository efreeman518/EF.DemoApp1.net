namespace Package.Infrastructure.AzureOpenAI;

public abstract class ChatServiceSettingsBase
{
    public string DeploymentName { get; set; } = null!;
    public string CacheName { get; set; } = null!;
}

namespace Package.Infrastructure.AzureOpenAI;

public abstract class ChatServiceSettingsBase
{
    public string Url { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public string CacheName { get; set; } = null!;

}

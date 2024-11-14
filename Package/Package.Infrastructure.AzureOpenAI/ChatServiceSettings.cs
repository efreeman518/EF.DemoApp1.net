namespace Package.Infrastructure.AzureOpenAI;

public class ChatServiceSettings
{
    public const string ConfigSectionName = "AzureOpenAIChatServiceSettings";
    public string Url { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;

}

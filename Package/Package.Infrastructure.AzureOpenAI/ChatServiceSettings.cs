namespace Package.Infrastructure.AzureOpenAI.ChatApi;

public class ChatServiceSettings
{
    public const string ConfigSectionName = "AzureOpenAIChatServiceSettings";
    public string Url { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;

}

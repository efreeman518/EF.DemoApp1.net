namespace Package.Infrastructure.AzureOpenAI.Assistant;

public abstract class AssistantServiceSettingsBase
{
    //public string ResourceName { get; set; } = null!;
    //public string DeploymentName { get; set; } = null!;
    public int RunThreadPollingDelayMilliseconds { get; set; } = 500;
}
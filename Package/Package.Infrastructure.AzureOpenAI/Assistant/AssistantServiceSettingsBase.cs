namespace Package.Infrastructure.AzureOpenAI.Assistant;

public abstract class AssistantServiceSettingsBase
{
    public string DeploymentName { get; set; } = null!;
}
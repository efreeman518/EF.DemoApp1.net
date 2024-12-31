namespace Package.Infrastructure.AzureOpenAI.Assistants;

public abstract class AssistantServiceSettingsBase
{
    //public string ResourceName { get; set; } = null!;
    //public string DeploymentName { get; set; } = null!;

    /// <summary>
    /// Most likely will have been created (in the portal or programmatically) for a specific assistant task/role
    /// </summary>
    public string? AssistantId { get; set; }
    public int RunThreadPollingDelayMilliseconds { get; set; } = 500;
}
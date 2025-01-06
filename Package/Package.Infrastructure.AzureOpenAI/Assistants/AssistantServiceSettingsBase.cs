namespace Package.Infrastructure.AzureOpenAI.Assistants;

public abstract class AssistantServiceSettingsBase
{
    //for polling, not async streaming 
    public int RunThreadPollingDelayMilliseconds { get; set; } = 500;

    //Currently required to accomodate the current experimental azureOpenAIClient.CreateAssistantAsync() method 
    //same as DeploymentName ?
    public string Model { get; set; } = null!;
}
namespace Application.Services.JobAssistant;

public class JobAssistantOrchestratorSettings
{
    public static string ConfigSectionName => "JobAssistantOrchestratorSettings";
    public string DeploymentName { get; set; } = null!;
    public int MaxJobSearchResults { get; set; } = 12;
}

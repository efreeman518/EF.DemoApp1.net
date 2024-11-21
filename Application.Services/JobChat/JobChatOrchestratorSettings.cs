namespace Application.Services.JobChat;

public class JobChatOrchestratorSettings
{
    public static string ConfigSectionName => "JobChatOrchestratorSettings";
    public int? MaxMessageCount { get; set; }
}

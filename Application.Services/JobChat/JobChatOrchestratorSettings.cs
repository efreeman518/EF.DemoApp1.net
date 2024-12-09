namespace Application.Services.JobChat;

public class JobChatOrchestratorSettings
{
    public static string ConfigSectionName => "JobChatOrchestratorSettings";
    public int? MaxCompletionMessageCount { get; set; }
    public int MaxToolCallRounds { get; set; } = 5;
    public int MaxJobSearchResults { get; set; } = 12;
}

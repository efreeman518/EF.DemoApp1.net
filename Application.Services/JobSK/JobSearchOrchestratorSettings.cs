namespace Application.Services.JobSK;

public class JobSearchOrchestratorSettings
{
    public static string ConfigSectionName => "JobSearchOrchestratorSettings";
    public int MaxJobSearchResults { get; set; } = 12;
    public string CacheName { get; set; } = null!;
}

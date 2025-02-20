namespace Application.Services.JobSK;

public class JobSearchOrchestratorSettings
{
    public static string ConfigSectionName => "JobSearchOrchestratorSettings";
    //public string ChatDeploymentName { get; set; } = null!;
    //public string EmbeddingDeploymentName { get; set; } = null!;

    public int MaxJobSearchResults { get; set; } = 12;
    public string CacheName { get; set; } = null!;

    //temp - async load so needed in the service instead of startup
    public string TodoOpenApiDocUrl { get; set; } = null!;  
}

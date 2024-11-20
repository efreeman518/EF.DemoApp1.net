namespace Infrastructure.JobsApi;
public class JobsApiServiceSettings
{
    public const string ConfigSectionName = "JobsApiServiceSettings";
    public string BaseUrl { get; set; } = null!;
    public string CacheName { get; set; } = null!;
}

namespace Infrastructure.JobsApi;
public class JobsServiceSettings
{
    public const string ConfigSectionName = "JobsServiceSettings";
    public string BaseUrl { get; set; } = null!;
}

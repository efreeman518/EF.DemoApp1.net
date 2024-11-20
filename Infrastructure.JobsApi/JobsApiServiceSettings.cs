namespace Infrastructure.JobsApi;
public class JobsApiServiceSettings
{
    public const string ConfigSectionName = "JobsServiceSettings";
    public string BaseUrl { get; set; } = null!;
}

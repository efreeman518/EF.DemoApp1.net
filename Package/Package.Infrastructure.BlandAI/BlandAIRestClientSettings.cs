namespace Package.Infrastructure.BlandAI;

public class BlandAIRestClientSettings
{
    public const string ConfigSectionName = "BlandAIRestClientSettings";
    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
}

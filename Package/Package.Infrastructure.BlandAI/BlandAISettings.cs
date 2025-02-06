namespace Package.Infrastructure.BlandAI;

public class BlandAISettings
{
    public const string ConfigSectionName = "BlandAISettings";
    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string WebhookSigningSecret { get; set; } = null!;
}

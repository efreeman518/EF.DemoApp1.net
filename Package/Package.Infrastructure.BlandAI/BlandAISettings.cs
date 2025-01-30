namespace Package.Infrastructure.BlandAI
{
    public class BlandAISettings
    {
        public const string ConfigSectionName = "BlandAISettings";
        public string WebhookSigningSecret { get; set; } = null!;
    }
}

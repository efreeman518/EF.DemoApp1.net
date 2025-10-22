using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public record class WebhookEvent
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("category")]
    public string Category { get; set; } = null!;

    [JsonPropertyName("log_level")]
    public string LogLevel { get; set; } = null!;
}
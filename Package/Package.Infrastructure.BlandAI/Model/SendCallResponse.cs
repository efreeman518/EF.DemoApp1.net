using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public class SendCallResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    [JsonPropertyName("batch_id")]
    public string? BatchId { get; set; } // Use `string` to handle `null` values
}

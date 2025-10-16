using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public record SendCallRequest : BlandCallRequestSettings
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;
}

public record SendCallResponse : DefaultResponse
{
    [JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    [JsonPropertyName("batch_id")]
    public string? BatchId { get; set; }
}


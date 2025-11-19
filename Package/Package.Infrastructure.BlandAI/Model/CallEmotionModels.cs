using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public record EmotionData
{
    [JsonPropertyName("emotion")]
    public string? Emotion { get; set; }

    [JsonPropertyName("callId")]
    public string? CallId { get; set; }

    [JsonPropertyName("analyzedAt")]
    public DateTime? AnalyzedAt { get; set; }
}

public record CallEmotionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public EmotionData? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public record CallEmotionRequest
{
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = null!;
}

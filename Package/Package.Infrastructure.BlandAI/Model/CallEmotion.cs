using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;
public class EmotionData
{
    [JsonPropertyName("emotion")]
    public string? Emotion { get; set; }

    [JsonPropertyName("callId")]
    public string? CallId { get; set; }

    [JsonPropertyName("analyzedAt")]
    public DateTime? AnalyzedAt { get; set; }
}

public class CallEmotionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public EmotionData? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class CallEmotionRequest
{
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = null!;
}

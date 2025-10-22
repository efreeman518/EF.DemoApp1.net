using System.Text.Json;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public record class WebhookCitation
{
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("citations")]
    public List<WebhookCitationItem>? Citations { get; set; }
}

public record class WebhookCitationItem
{
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("variable_name")]
    public string VariableName { get; set; } = null!;

    [JsonPropertyName("variable_type")]
    public string VariableType { get; set; } = null!;

    // Value can be boolean, number, string or null in payloads
    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }

    [JsonPropertyName("cited_utterances")]
    public List<WebhookCitationUtterance>? CitedUtterances { get; set; }

    [JsonPropertyName("schema_id")]
    public string? SchemaId { get; set; }
}

public record class WebhookCitationUtterance
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("idx")]
    public int? Idx { get; set; }

    [JsonPropertyName("start_time")]
    public double? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public double? EndTime { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    [JsonPropertyName("channel")]
    public int? Channel { get; set; }

    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }

    [JsonPropertyName("speaker_id")]
    public string? SpeakerId { get; set; }

    [JsonPropertyName("speaker_name")]
    public string? SpeakerName { get; set; }

    [JsonPropertyName("speaker_description")]
    public string? SpeakerDescription { get; set; }

    [JsonPropertyName("topics")]
    public List<string>? Topics { get; set; }

    // Provided as a JSON string in the payload
    [JsonPropertyName("topics_meta")]
    public string? TopicsMeta { get; set; }

    [JsonPropertyName("utterance_type")]
    public string? UtteranceType { get; set; }
}
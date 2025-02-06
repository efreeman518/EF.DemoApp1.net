using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

/// <summary>
/// https://docs.bland.ai/api-v1/post/agents
/// </summary>
public class AgentRequest
{
    //used for update in url
    public string? AgentId { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    [JsonPropertyName("webhook")]
    public string Webhook { get; set; } = null!;

    [JsonPropertyName("analysis_schema")]
    public Dictionary<string, object> AnalysisSchema { get; set; } = [];

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];

    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    //English: ENG
    //Spanish: ESP
    //French: FRE
    //Polish: POL
    //German: GER
    //Italian: ITA
    //Brazilian Portuguese: PBR
    //Portuguese: POR
    [JsonPropertyName("language")]
    public string Language { get; set; } = "ENG";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "enhanced"; //base, turbo and enhanced

    [JsonPropertyName("first_sentence")]
    public string? FirstSentence { get; set; }

    [JsonPropertyName("tools")]
    public List<Dictionary<string, object>>? Tools { get; set; }

    [JsonPropertyName("dynamic_data")]
    public Dictionary<string, object>? DynamicData { get; set; }

    [JsonPropertyName("interruption_threshold")]
    public int InterruptionThreshold { get; set; } = 100;

    //create only; not used in update request
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];

    [JsonPropertyName("max_duration")]
    public int MaxDuration { get; set; } = 10;
}

public class AgentResponse
{
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("dynamic_data")]
    public object? DynamicData { get; set; }

    [JsonPropertyName("interruption_threshold")]
    public int? InterruptionThreshold { get; set; }

    [JsonPropertyName("first_sentence")]
    public string? FirstSentence { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("voice_settings")]
    public object? VoiceSettings { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("temperature")]
    public object? Temperature { get; set; }

    [JsonPropertyName("max_duration")]
    public int MaxDuration { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("tools")]
    public object? Tools { get; set; }
}

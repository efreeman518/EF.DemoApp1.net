using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public record class StandardPostCallWebhook
{
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("c_id")]
    public string? CId { get; set; }

    [JsonPropertyName("call_length")]
    public double? CallLength { get; set; }

    [JsonPropertyName("batch_id")]
    public string? BatchId { get; set; }

    [JsonPropertyName("to")]
    public string To { get; set; } = null!;

    [JsonPropertyName("from")]
    public string From { get; set; } = null!;

    [JsonPropertyName("completed")]
    public bool? Completed { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("inbound")]
    public bool? Inbound { get; set; }

    [JsonPropertyName("queue_status")]
    public string? QueueStatus { get; set; }

    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("variables")]
    public object? Variables { get; set; }

    [JsonPropertyName("answered_by")]
    public string? AnsweredBy { get; set; }

    [JsonPropertyName("record")]
    public bool? Record { get; set; }

    [JsonPropertyName("recording_url")]
    public string? RecordingUrl { get; set; }

    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("price")]
    public double? Price { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("local_dialing")]
    public bool? LocalDialing { get; set; }

    [JsonPropertyName("call_ended_by")]
    public string? CallEndedBy { get; set; }

    [JsonPropertyName("pathway_logs")]
    public List<PathwayLog>? PathwayLogs { get; set; }

    [JsonPropertyName("transferred_to")]
    public string? TransferredTo { get; set; }

    [JsonPropertyName("pre_transfer_duration")]
    public double? PreTransferDuration { get; set; }

    [JsonPropertyName("post_transfer_duration")]
    public double? PostTransferDuration { get; set; }

    [JsonPropertyName("pathway_tags")]
    public List<string>? PathwayTags { get; set; }

    [JsonPropertyName("recording_expiration")]
    public DateTime? RecordingExpiration { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    [JsonPropertyName("is_proxy_agent_call")]
    public bool? IsProxyAgentCall { get; set; }

    [JsonPropertyName("citation_schema_ids")]
    public List<string>? CitationSchemaIds { get; set; }

    [JsonPropertyName("warm_transfer_call")]
    public WarmTransferCall? WarmTransferCall { get; set; }

    [JsonPropertyName("concatenated_transcript")]
    public string? ConcatenatedTranscript { get; set; }

    [JsonPropertyName("transcripts")]
    public List<Transcript>? Transcripts { get; set; }

    [JsonPropertyName("corrected_duration")]
    public string? CorrectedDuration { get; set; }

    [JsonPropertyName("end_at")]
    public DateTime? EndAt { get; set; }

    [JsonPropertyName("disposition_tag")]
    public string? DispositionTag { get; set; }
}

public record class PathwayLog
{
    [JsonPropertyName("tag")]
    public PathwayLogTag? Tag { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("decision")]
    public string? Decision { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("pathway_info")]
    public string? PathwayInfo { get; set; }

    [JsonPropertyName("chosen_node_id")]
    public string? ChosenNodeId { get; set; }
}

public record class PathwayLogTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}
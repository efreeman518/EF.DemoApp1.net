using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public class RequestData
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("wait")]
    public bool? Wait { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = null!;
}

public class CallDetails
{
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = null!;

    [JsonPropertyName("call_length")]
    public double? CallLength { get; set; }

    [JsonPropertyName("batch_id")]
    public string? BatchId { get; set; }

    [JsonPropertyName("to")]
    public string To { get; set; } = null!;

    [JsonPropertyName("from")]
    public string From { get; set; } = null!;

    [JsonPropertyName("request_data")]
    public RequestData? RequestData { get; set; }

    [JsonPropertyName("completed")]
    public bool? Completed { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("inbound")]
    public bool? Inbound { get; set; }

    [JsonPropertyName("queue_status")]
    public string QueueStatus { get; set; } = null!;

    [JsonPropertyName("endpoint_url")]
    public string EndpointUrl { get; set; } = null!;

    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }

    [JsonPropertyName("error_message")]
    public object? ErrorMessage { get; set; }

    [JsonPropertyName("variables")]
    public KeyValuePair<string, object>? Variables { get; set; }

    [JsonPropertyName("answered_by")]
    public string? AnsweredBy { get; set; }

    [JsonPropertyName("record")]
    public bool? Record { get; set; }

    [JsonPropertyName("recording_url")]
    public string? RecordingUrl { get; set; }

    [JsonPropertyName("c_id")]
    public string? CId { get; set; }

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
    public object? PathwayLogs { get; set; }

    [JsonPropertyName("analysis_schema")]
    public object? AnalysisSchema { get; set; }

    [JsonPropertyName("analysis")]
    public object? Analysis { get; set; }

    [JsonPropertyName("concatenated_transcript")]
    public string? ConcatenatedTranscript { get; set; }

    [JsonPropertyName("transcripts")]
    public List<Transcript>? Transcripts { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("corrected_duration")]
    public string? CorrectedDuration { get; set; }

    [JsonPropertyName("end_at")]
    public DateTime? EndAt { get; set; }
}

public class Transcript
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("c_id")]
    public string? CId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("transcript_id")]
    public string? TranscriptId { get; set; }
}
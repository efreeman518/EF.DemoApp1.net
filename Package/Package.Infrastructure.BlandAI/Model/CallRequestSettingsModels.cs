using Package.Infrastructure.BlandAI.Enums;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

/// <summary>
/// Common settings for making a call request to the Bland AI API.
/// </summary>
public record BlandCallRequestSettings 
{
    // Call settings properties
    //https://docs.bland.ai/api-v1/post/calls

    //phone_number will be specified dynamically for each call

    //public bool RecordCalls { get; set; } = true; // Default setting for recording calls


    [JsonPropertyName("from")]
    public string? From { get; set; } //if using a known Twilio number

    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    [JsonPropertyName("task")]
    public string? Task { get; set; }

    //Maya Josh Florian Derek June Nat Paige
    [JsonPropertyName("voice")]
    public string? Voice { get; set; } = "Maya"; // Default voice name for calls

    /// <summary>
    /// null - Default, will play audible but quiet phone static.
    /// office - Office-style soundscape.Includes faint typing, chatter, clicks, and other office sounds.
    /// cafe - Cafe-like soundscape. Includes faint talking, clinking, and other cafe sounds.
    /// restaurant - Similar to cafe, but more subtle.
    /// none - Minimizes background noise
    /// </summary>
    [JsonPropertyName("background_track")]
    public string? BackgroundTrack { get; set; } = "none";

    [JsonPropertyName("first_sentence")]
    public string? FirstSentence { get; set; }

    [JsonPropertyName("wait_for_greeting")]
    public bool WaitForGreeting { get; set; }

    [JsonPropertyName("block_interruptions")]
    public bool BlockInterruptions { get; set; }

    [JsonPropertyName("interruption_threshold")]
    public int? InterruptionThreshold { get; set; } = 100;

    //base turbo
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("temperature")]
    public decimal? Temperature { get; set; }

    [JsonPropertyName("dynamic_data")]
    public object[]? DynamicData { get; set; } = null; // Default dynamic data for the model; EF configuration will need .HasConversion()

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("pronunciation_guide")]
    public List<PronunciationGuide>? PronunciationGuide { get; set; }

    [JsonPropertyName("transfer_phone_number")]
    public string? TransferPhoneNumber { get; set; }

    //Overrides transfer_phone_number if a TransferList.default is specified.
    /// <summary>
    /// "transfer_list": {
    ///  "default": "+12223334444",
    ///  "sales": "+12223334444",
    ///  "support": "+12223334444",
    ///  "billing": "+12223334444"
    ///}
    /// </summary>
    [JsonPropertyName("transfer_list")]
    public Dictionary<string, string>? TransferList { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; } = "en-US"; // Default language for the call

    [JsonPropertyName("pathway_version")]
    public int? PathwayVersion { get; set; }

    [JsonPropertyName("local_dialing")]
    public bool LocalDialing { get; set; }

    [JsonPropertyName("voicemail_sms")]
    public object? VoicemailSms { get; set; } //  If a voicemail is left, an SMS message is sent to the “to” number. Requires SMS configuration.

    [JsonPropertyName("dispatch_hours")]
    public DispatchHours DispatchHours { get; set; } = new()
    {
        Start = TimeOnly.ParseExact("09:00", "HH:mm", CultureInfo.InvariantCulture),
        End = TimeOnly.ParseExact("17:00", "HH:mm", CultureInfo.InvariantCulture)
    }; // Restricts calls to certain hours in your timezone. Specify the timezone and time windows using 24-hour format.

    [JsonPropertyName("sensitive_voicemail_detection")]
    public bool SensitiveVoicemailDetection { get; set; }

    [JsonPropertyName("noise_cancellation")]
    public bool NoiseCancellation { get; set; }

    [JsonPropertyName("ignore_button_press")]
    public bool IgnoreButtonPress { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; } = "America/Los_Angeles";

    [JsonPropertyName("request_data")]
    public Dictionary<string, string>? RequestData { get; set; } //provide replacement vars for the call {{name}}, {{company}}, etc. This is used to replace variables in the AI's dialog.

    [JsonPropertyName("tools")]
    public object[]? Tools { get; set; } // Default dynamic data for the model; EF configuration will need .HasConversion()

    /// <summary>
    /// YYYY-MM-DD HH:MM:SS -HH:MM
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("voicemail_message")]
    public string? VoicemailMessage { get; set; }

    /// <summary>
    /// • hangup • leave_message • ignore
    /// </summary>
    [JsonPropertyName("voicemail_action")]
    public string? VoicemailAction { get; set; }

    // If the AI encounters a voicemail, it will retry the call after the specified wait time. If voicemail_action is set to hangup, the AI will not retry the call.
    [JsonPropertyName("retry")]
    public CallRetry? Retry { get; set; }

    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; } // Maximum duration of the call in minutes (default is 30)

    [JsonPropertyName("record")]
    public bool RecordCallAudio { get; set; } //access through the recording_url field in the call details 

    [JsonPropertyName("webhook")]
    public string? Webhook { get; set; }

    /// <summary>
    /// queue, call, latency, webhook, tool, dynamic_data, citations (Sent separately, Enterprise only)
    /// </summary>
    [JsonPropertyName("webhook_events")]
    public string[]? WebhookEvents { get; set; } //Anything here will be returned in the post call webhook under metadata.

    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }

    [JsonPropertyName("citation_schema_id")]
    public string? CitationSchemaId { get; set; } //Enterprise only

    [JsonPropertyName("analysis_preset")]
    public string? AnalysisPreset { get; set; }

    //human: voicemail: unknown: no-answer: Call was not answered null: Detection not enabled or still processing
    [JsonPropertyName("answered_by_enabled")]
    public bool? AnswerByEnabled { get; set; }

    [JsonPropertyName("available_tags")]
    public string[]? AvailableTags { get; set; }

    [JsonPropertyName("geospatial_dialing")]
    public string? GeoSpatialDialing { get; set; }

    [JsonPropertyName("precall_dtmf_sequence")]
    public string? PrecallDTMFSequence { get; set; }

}

public record PronunciationGuide
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = null!;

    [JsonPropertyName("pronunciation")]
    public string Pronunciation { get; set; } = null!;

    [JsonPropertyName("case_sensitive")]
    public bool CaseSensitive { get; set; }

    [JsonPropertyName("spaced")]
    public bool Spaced { get; set; }
}

public record DispatchHours
{
    //"09:00"
    [JsonPropertyName("start")]
    public TimeOnly Start { get; set; }

    // "17:00"
    [JsonPropertyName("end")]
    public TimeOnly End { get; set; }
}

public record CallRetry
{

    [JsonPropertyName("wait")]
    public int Wait { get; set; } = 30; // Default wait time in seconds before retrying the call

    //hangup, leave_message, ignore
    [JsonPropertyName("voicemail_action")]
    public VoicemailAction VoicemailAction { get; set; } = VoicemailAction.hangup;

    [JsonPropertyName("voicemail_message")]
    public string VoicemailMessage { get; set; } = null!;

}

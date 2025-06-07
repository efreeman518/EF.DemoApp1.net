using Package.Infrastructure.BlandAI.Enums;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public class SendCallRequest
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    [JsonPropertyName("task")]
    public string? Task { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    /// <summary>
    /// null - Default, will play audible but quiet phone static.
    /// office - Office-style soundscape.Includes faint typing, chatter, clicks, and other office sounds.
    /// cafe - Cafe-like soundscape. Includes faint talking, clinking, and other cafe sounds.
    /// restaurant - Similar to cafe, but more subtle.
    /// none - Minimizes background noise
    /// </summary>
    [JsonPropertyName("background_track")]
    public string? BackgroundTrack { get; set; }

    [JsonPropertyName("first_sentence")]
    public string? FirstSentence { get; set; }

    [JsonPropertyName("wait_for_greeting")]
    public bool WaitForGreeting { get; set; }

    [JsonPropertyName("block_interruptions")]
    public bool BlockInterruptions { get; set; }

    [JsonPropertyName("interruption_threshold")]
    public int? InterruptionThreshold { get; set; }

    /// <summary>
    /// default:"enhanced"
    /// </summary>
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
    public string? Language { get; set; }

    [JsonPropertyName("pathway_version")]
    public int? PathwayVersion { get; set; }

    [JsonPropertyName("local_dialing")]
    public bool LocalDialing { get; set; }

    [JsonPropertyName("voicemail_sms")]
    public string? VoicemailSms { get; set; }

    [JsonPropertyName("dispatch_hours")]
    public DispatchHours? DispatchHours { get; set; }

    [JsonPropertyName("sensitive_voicemail_detection")]
    public bool SensitiveVoicemailDetection { get; set; }

    [JsonPropertyName("noise_cancellation")]
    public bool NoiseCancellation { get; set; }

    [JsonPropertyName("ignore_button_press")]
    public bool IgnoreButtonPress { get; set; }

    [JsonPropertyName("language_detection_period")]
    public int? LanguageDetectionPeriod { get; set; }

    [JsonPropertyName("language_detection_options")]
    public List<string>? LanguageDetectionOptions { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("request_data")]
    public Dictionary<string, string>? RequestData { get; set; }

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

    [JsonPropertyName("retry")]
    public CallRetry? Retry { get; set; }

    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }

    [JsonPropertyName("record")]
    public bool Record { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("webhook")]
    public string? Webhook { get; set; }

    /// <summary>
    /// queue, call, latency, webhook, tool, dynamic_data
    /// </summary>
    [JsonPropertyName("webhook_events")]
    public List<string>? WebhookEvents { get; set; }

    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }

    [JsonPropertyName("analysis_preset")]
    public string? AnalysisPreset { get; set; }

    [JsonPropertyName("available_tags")]
    public List<string>? AvailableTags { get; set; }
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
using Package.Infrastructure.BlandAI.Enums;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

/// <summary>
/// Common settings for making a call request to the Bland AI API.
/// </summary>
public record BlandCallRequestSettings
{
    // Call settings properties
    //https://docs.bland.ai/api-v1/post/calls

    //Basic Paramters

    //phone_number will be specified dynamically for each call

    /// <summary>
    /// Maya Josh Florian Derek June Nat Paige
    /// </summary>
    [JsonPropertyName("voice")]
    public string? Voice { get; set; } = "Maya"; // Default voice name for calls

    /// <summary>
    /// Optional pathway ID to use for the call. If not specified, task (prompt) will be used.
    /// </summary>
    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }

    /// <summary>
    /// Defaults to prod version
    /// </summary>
    [JsonPropertyName("pathway_version")]
    public int? PathwayVersion { get; set; }

    /// <summary>
    /// The prompt - Agent instructions to guide the AI's behavior during the call. Do not specify if using a pathway.
    /// </summary>
    [JsonPropertyName("task")]
    public string? Task { get; set; }

    /// <summary>
    /// Makes your agent say a specific phrase or sentence for it’s first response.
    /// </summary>
    [JsonPropertyName("first_sentence")]
    public string? FirstSentence { get; set; }

    /// <summary>
    /// pre-configured templates in Bland AI
    /// </summary>
    [JsonPropertyName("persona_id")]
    public string? PersonaId { get; set; }

    //Model Parameters

    /// <summary>
    /// base turbo
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Optimizes the Bland API for that language - transcription, speech, and other inner workings.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; } = "en-US"; // Default language for the call

    /// <summary>
    /// By default, the agent starts talking as soon as the call connects.
    /// </summary>
    [JsonPropertyName("wait_for_greeting")]
    public bool WaitForGreeting { get; set; }

    /// <summary>
    /// Guides the agent on how to say specific words
    /// </summary>
    [JsonPropertyName("pronunciation_guide")]
    public PronunciationGuide[]? PronunciationGuide { get; set; }

    /// <summary>
    /// A value between 0 and 1 that controls the randomness of the LLM. 0 will cause more deterministic outputs while 1 will cause more random.
    /// </summary>
    [JsonPropertyName("temperature")]
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Adjusts how patient the AI is when waiting for the user to finish speaking.
    /// Lower values mean the AI will respond more quickly, while higher values mean the AI will wait longer before responding.
    /// </summary>
    [JsonPropertyName("interruption_threshold")]
    public int? InterruptionThreshold { get; set; } = 100;


    //Dispatch Parameters

    /// <summary>
    /// Specify a phone number to call from that you own
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; } //if using a known Twilio number

    /// <summary>
    /// Controls how the caller number (from) is selected when placing an outbound call.
    /// By default, Bland will choose a US-based number from our own pool of numbers.
    /// local - Automatically selects a from number that matches the callee’s area code for US-based calls. You must have purchased a local dialing add-on in the add-ons section.
    /// custom_pooling - Selects a number from your own pre-configured pool of phone numbers.
    /// </summary>
    [JsonPropertyName("dialing_strategy")]
    public string? DialingStrategy { get; set; } //if using a known Twilio number

    /// <summary>
    /// Set the timezone for the call. Handled automatically for calls in the US.
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; } = "America/Los_Angeles";

    /// <summary>
    /// The time you want the call to start. If you don’t specify a time (or the time is in the past), the call will send immediately.
    /// YYYY-MM-DD HH:MM:SS -HH:MM  (ex. 2021-01-01 12:00:00 -05:00).
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// A phone number that the agent can transfer to under specific conditions - such as being asked to speak to a human or supervisor. This option will be ignored for pathways.
    /// In the task, refer to the action solely as “transfer” or “transferring”.
    /// </summary>
    [JsonPropertyName("transfer_phone_number")]
    public string? TransferPhoneNumber { get; set; }

    /// <summary>
    /// Overrides transfer_phone_number if a TransferList.default is specified.
    /// "transfer_list": {
    ///  "default": "+12223334444",
    ///  "sales": "+12223334444",
    ///  "support": "+12223334444",
    ///  "billing": "+12223334444"
    ///}
    /// </summary>
    [JsonPropertyName("transfer_list")]
    public Dictionary<string, string>? TransferList { get; set; }

    /// <summary>
    /// Maximum duration of the call in minutes (default is 30)
    /// </summary>
    [JsonPropertyName("max_duration")]
    public int? MaxDuration { get; set; }

    //Knowledge Parameters

    /// <summary>
    /// Add custom tools and knowledge bases to your call for your agent to call upon.
    /// https://docs.bland.ai/tutorials/custom-tools#custom-tools
    /// {
    /// "tools": [
    ///   "TL-ba6c4237-67c2-40e8-868b-60d429a84eda",
    ///    "KB-30d465c0-22d0-41e0-a63d-61bcacc277e7"
    ///  ]
    ///}
    /// </summary>
    [JsonPropertyName("tools")]
    public object[]? Tools { get; set; } // Default dynamic data for the model; EF configuration will need .HasConversion()

    //Audio Parameters

    /// <summary>
    /// null - Default, will play audible but quiet phone static.
    /// office - Office-style soundscape.Includes faint typing, chatter, clicks, and other office sounds.
    /// cafe - Cafe-like soundscape. Includes faint talking, clinking, and other cafe sounds.
    /// restaurant - Similar to cafe, but more subtle.
    /// none - Minimizes background noise
    /// </summary>
    [JsonPropertyName("background_track")]
    public string? BackgroundTrack { get; set; } = "none";


    /// <summary>
    /// Toggles noise filtering or suppression in the audio stream to filter out background noise.
    /// </summary>
    [JsonPropertyName("noise_cancellation")]
    public bool NoiseCancellation { get; set; }

    /// <summary>
    /// When set to true, the AI will not respond or process interruptions from the user.
    /// </summary>
    [JsonPropertyName("block_interruptions")]
    public bool BlockInterruptions { get; set; }

    /// <summary>
    /// access through the recording_url field in the call details 
    /// </summary>
    [JsonPropertyName("record")]
    public bool RecordCallAudio { get; set; }

    //Voicemail Parameters

    /// <summary>
    /// Configuration for handling voicemails during outbound calls. This object controls how the AI behaves when it encounters a voicemail, including whether to leave a message, send an SMS notification, or detect voicemails more intelligently using AI.
    /// {
    ///  "voicemail": {
    ///    "message": "Hi, just calling to follow up. Please call us back when you can.",
    ///    "action": "leave_message",
    ///    "sms": {
    ///      "to": "+18005550123",
    ///      "from": "+18005550678",
    ///      "message": "We just left you a voicemail. Call us back anytime!"
    ///    },
    ///    "sensitive": true
    ///  }
    ///}

    /// </summary>
    [JsonPropertyName("voicemail")]
    public VoiceMail? Voicemail { get; set; }

    //Analysis Parameters

    /// <summary>
    /// tools for running post call analysis, including specific variable extractions, conditional logic, and more. enterprise-only feature
    /// https://app.bland.ai/dashboard/analytics?tab=citations
    /// </summary>
    [JsonPropertyName("citation_schema_ids")]
    public string[]? CitationSchemaId { get; set; }

    //Post Call Parameters

    /// <summary>
    /// (Optional) Custom instructions for how the call summary should be generated after the call completes. Use this to provide specific guidance or context for the AI when writing the post-call summary. Maximum length: 2000 characters.
    /// 
    /// </summary>
    [JsonPropertyName("summary_prompt")]
    public string? SummaryPrompt { get; set; }

    /// <summary>
    /// If the AI encounters a voicemail, it will retry the call after the specified wait time. If voicemail_action is set to hangup, the AI will not retry the call.
    /// </summary>
    [JsonPropertyName("retry")]
    public CallRetry? Retry { get; set; }

    /// <summary>
    /// A list of possible outcome tags (dispositions) you define. After the call ends, the AI reviews the transcript and picks one of these tags to describe how the call went.
    /// {
    ///  "dispositions": ["got_full_name_and_number", "no_information_provided", "transferred_to_agent"]
    /// }
    /// </summary>
    [JsonPropertyName("dispositions")]
    public string[]? Dispositions { get; set; }

    //Advanced Parameters

    /// <summary>
    /// Custom key-value data you send with the call. This information is available as variables inside your prompt, pathway, or tools — but only if the call is answered.
    /// provide replacement vars for the call {{name}}, {{company}}, etc. This is used to replace variables in the AI's dialog.
    /// </summary>
    [JsonPropertyName("request_data")]
    public Dictionary<string, string>? RequestData { get; set; }

    /// <summary>
    /// Add any additional information you want to associate with the call. This data is accessible for all calls, regardless of if they are picked up or not. This can be used to track calls or add custom data to the call.
    /// Anything that you put here will be returned in the post call webhook under metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }

    /// <summary>
    /// When the call ends, call information is sent to this webhook URL.
    /// </summary>
    [JsonPropertyName("webhook")]
    public string? Webhook { get; set; }

    /// <summary>
    /// queue, call, latency, webhook, tool, dynamic_data, citations (Sent separately, Enterprise only)
    /// </summary>
    [JsonPropertyName("webhook_events")]
    public string[]? WebhookEvents { get; set; } //Anything here will be returned in the post call webhook under metadata.

    /// <summary>
    /// Integrate data from external APIs into your agent’s knowledge.
    /// efault dynamic data for the model; EF configuration will need .HasConversion()
    /// </summary>
    [JsonPropertyName("dynamic_data")]
    public object[]? DynamicData { get; set; } = null; // D

    /// <summary>
    /// These words will be boosted in the transcription engine - recommended for proper nouns or words that are frequently mis-transcribed.
    /// </summary>
    [JsonPropertyName("keywords")]
    public string[]? Keywords { get; set; }

    /// <summary>
    /// This disables any in-call actions triggered by keypad input, such as menu navigation or transfers.
    /// Useful when your agent should handle the entire call conversationally, without responding to button presses.
    /// </summary>
    [JsonPropertyName("ignore_button_press")]
    public bool IgnoreButtonPress { get; set; }

    /// <summary>
    /// A sequence of DTMF digits that will be played before the call starts. Acceptable characters are 0-9, *, #, and w, where w is a pause of 0.5 seconds. Example:
    /// </summary>
    [JsonPropertyName("precall_dtmf_sequence")]
    public string? PrecallDTMFSequence { get; set; }
}

public record VoiceMail
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// hangup, leave_message, ignore
    /// </summary>
    [JsonPropertyName("action")]
    public VoicemailAction Action { get; set; } = VoicemailAction.hangup;
    [JsonPropertyName("sms")]
    public VoicemailSms? Sms { get; set; }
    [JsonPropertyName("sensitive")]
    public bool Sensitive { get; set; }
}

public record VoicemailSms
{
    [JsonPropertyName("to")]
    public string To { get; set; } = null!;
    [JsonPropertyName("from")]
    public string From { get; set; } = null!;
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
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

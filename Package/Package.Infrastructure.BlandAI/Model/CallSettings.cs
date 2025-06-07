using Package.Infrastructure.BlandAI.Enums;
using System.Globalization;

namespace Package.Infrastructure.BlandAI.Model;

public record CallSettings 
{
    // Call settings properties
    //https://docs.bland.ai/api-v1/post/calls

    //Maya Josh Florian Derek June Nat Paige
    public string VoiceName { get; set; } = "Maya"; // Default voice name for calls
    public bool RecordCalls { get; set; } = true; // Default setting for recording calls

    /*
     *  null - Default, will play audible but quiet phone static.
        office - Office-style soundscape. Includes faint typing, chatter, clicks, and other office sounds.
        cafe - Cafe-like soundscape. Includes faint talking, clinking, and other cafe sounds.
        restaurant - Similar to cafe, but more subtle.
        none - Minimizes background noise
     */
    public string BackgroundTrack { get; set; } = "none"; // Default background track for calls

    public bool WaitForGreeting { get; set; } = false; // Default setting for waiting for greeting before starting the call
    public int InterruptionThreshold { get; set; } = 100; // Default threshold for interruptions in the call

    //base turbo
    public string Model { get; set; } = "base"; // Default model for the tenant
    public float Temperature { get; set; } = 0.7f; // Default temperature for the model
    public object[]? DynamicData { get; set; } = null; // Default dynamic data for the model
    public string[]? Keywords { get; set; } = null; // Default keywords for the model
    public PronunciationGuide? PronounciationGuide { get; set; } = null; // Default pronunciation guide for the model
    public string TransferPhoneNumber { get; set; } = null!; // Default transfer phone number for the tenant

    //Overrides transfer_phone_number if a TransferList.default is specified.
    public string[]? TransferList { get; set; } = null; // Default transfer list for the tenant
    public string Language { get; set; } = "en-US"; // Default language for the tenant
    public string? VoicemailSMS { get; set; } = null; //  If a voicemail is left, an SMS message is sent to the “to” number. Requires SMS configuration.
    public DispatchHours DispatchHours { get; set; } = new()
    {
        Start = TimeOnly.ParseExact("09:00", "HH:mm", CultureInfo.InvariantCulture),
        End = TimeOnly.ParseExact("17:00", "HH:mm", CultureInfo.InvariantCulture)
    }; // Restricts calls to certain hours in your timezone. Specify the timezone and time windows using 24-hour format.

    public bool SensitiveVoicemailDetection { get; set; } = false; // uses LLM-based analysis to detect frequent voicemails.
    public bool NoiseCancellation { get; set; } = false; // Toggles noise filtering or suppression in the audio stream to filter out background noise.
    public bool IgnoreButtonPresses { get; set; } = false; // When true, DTMF (digit) presses are ignored, disabling menu navigation or call transfers triggered by keypad input.
    public string Timezone { get; set; } = "America/Los_Angeles"; // Default timezone for calls
    public string VoicemailMessage { get; set; } = null!; //When the AI encounters a voicemail, it will leave this message after the beep and then immediately end the call.
    public VoicemailAction VoicemailAction { get; set; } = VoicemailAction.hangup; // This is processed separately from the AI’s decision making, and overrides it.
    public CallRetry? Retry { get; set; } = null; // If the AI encounters a voicemail, it will retry the call after the specified wait time. If voicemail_action is set to hangup, the AI will not retry the call.

    //When the call starts, a timer is set for the max_duration minutes. At the end of that timer, if the call is still active it will be automatically ended.
    public int MaxDuration { get; set; } = 20; // Maximum duration of the call in seconds (default is 1 hour)
    public bool EnableCallRecording { get; set; } = true; // Whether to enable call recording for the tenant

}

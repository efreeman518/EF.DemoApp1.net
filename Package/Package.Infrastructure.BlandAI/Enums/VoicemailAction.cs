namespace Package.Infrastructure.BlandAI.Enums;

/// <summary>
/// If voicemail_message is set, then the AI will leave the message regardless of the voicemail_action.
/// </summary>
public enum VoicemailAction
{
    hangup,        // The call will be disconnected without leaving a message.
    leave_message,  // A message will be left on the voicemail.
    ignore         // The call will be ignored, and no action will be taken.
}

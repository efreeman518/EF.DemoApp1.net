namespace Application.Contracts.Model;

public class AssistantResponse(string assistantId, string threadId, string message)
{
    public string AssistantId { get; set; } = assistantId;
    public string ThreadId { get; set; } = threadId;
    public string Message { get; set; } = message;

}

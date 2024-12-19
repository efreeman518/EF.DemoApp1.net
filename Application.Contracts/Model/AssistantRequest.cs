namespace Application.Contracts.Model;
public class AssistantRequest
{
    public string? AssistantId { get; set; }
    public string? ThreadId { get; set; }
    public string Message { get; set; } = null!;
}

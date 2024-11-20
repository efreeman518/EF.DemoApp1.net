namespace Application.Contracts.Model;
public class ChatRequest
{
    public Guid? ChatId { get; set; }
    public string Message { get; set; } = null!;
}

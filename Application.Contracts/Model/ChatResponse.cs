namespace Application.Contracts.Model;

public class ChatResponse(Guid id, string message)
{
    public Guid ChatId { get; set; } = id;
    public string Message { get; set; } = message;

}

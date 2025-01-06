namespace Package.Infrastructure.Test.Integration.Model;


public partial class TodoItemDto
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public TodoItemStatus Status { get; set; }
}

using Domain.Shared.Enums;
using Package.Infrastructure.Data;

namespace Domain.Model;

public class TodoItem : EntityBase
{
    public string Name { get; set; } = null!;
    public bool IsComplete { get; set; }
    public TodoItemStatus Status { get; set; }
}

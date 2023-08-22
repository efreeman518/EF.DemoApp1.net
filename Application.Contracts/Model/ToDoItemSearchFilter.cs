using Domain.Shared.Enums;

namespace Application.Contracts.Model;

public class TodoItemSearchFilter
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public List<TodoItemStatus>? Statuses { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
}

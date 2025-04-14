namespace SampleApp.UI1.Model;

public record TodoItemSearchFilter
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public List<TodoItemStatus>? Statuses { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }

    public TodoItemSearchFilter() { }

    public TodoItemSearchFilter(Guid? id, string? name, List<TodoItemStatus>? statuses, DateTime? dateStart, DateTime? dateEnd)
    {
        Id = id;
        Name = name;
        Statuses = statuses;
        DateStart = dateStart;
        DateEnd = dateEnd;
    }
}
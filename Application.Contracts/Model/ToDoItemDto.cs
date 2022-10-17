using System;

namespace Application.Contracts.Model;

public class TodoItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsComplete { get; set; }
}

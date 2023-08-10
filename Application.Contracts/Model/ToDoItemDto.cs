using Domain.Shared.Enums;
using System;

namespace Application.Contracts.Model;

public class TodoItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public TodoItemStatus Status { get; set; }
    public string? SecureRandom { get; set; }
    public string? SecureDeterministic { get; set; }

}

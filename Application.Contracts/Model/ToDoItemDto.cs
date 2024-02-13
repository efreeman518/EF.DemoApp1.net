using Domain.Shared.Enums;

namespace Application.Contracts.Model;

public record TodoItemDto(Guid? Id, string Name, TodoItemStatus Status, string? SecureRandom = null, string? SecureDeterministic = null);


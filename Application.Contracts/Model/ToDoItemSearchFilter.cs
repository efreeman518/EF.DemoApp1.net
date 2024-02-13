using Domain.Shared.Enums;

namespace Application.Contracts.Model;

public record TodoItemSearchFilter(Guid? Id = null, string? Name = null, List<TodoItemStatus>? Statuses = null, DateTime? DateStart = null, DateTime? DateEnd = null);


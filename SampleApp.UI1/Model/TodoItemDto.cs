namespace SampleApp.UI1.Model;

public record TodoItemDto(Guid? Id = null, string Name = "default name", TodoItemStatus Status = TodoItemStatus.Created, string? SecureRandom = null, string? SecureDeterministic = null);


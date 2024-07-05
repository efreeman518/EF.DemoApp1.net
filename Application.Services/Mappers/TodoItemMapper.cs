namespace Application.Services.Mappers;
public static class TodoItemMapper
{
    public static TodoItemDto ToDto(TodoItem item)
    {
        return new TodoItemDto(item.Id, item.Name, item.Status, item.SecureRandom, item.SecureDeterministic);
    }

    public static TodoItem ToEntity(TodoItemDto dto)
    {
        return new TodoItem(dto.Name, dto.Status, dto.SecureRandom, dto.SecureDeterministic)
        {
            Id = dto.Id ?? Guid.Empty
        };
    }
}

using Application.Contracts.Model;
using Domain.Model;

namespace Application.Contracts.Mappers;

public static class TodoItemMapper
{
    public static TodoItemDto ToDto(this TodoItem item)
    {
        return new TodoItemDto(item.Id, item.Name, item.Status, item.SecureRandom, item.SecureDeterministic);
    }

    public static TodoItem ToEntity(this TodoItemDto dto)
    {
        return new TodoItem(dto.Name, dto.Status, dto.SecureRandom, dto.SecureDeterministic)
        {
            Id = dto.Id ?? Guid.Empty
        };
    }

    public static TodoItemDto Projector(TodoItem item)
    {
        return item.ToDto();
    }
}

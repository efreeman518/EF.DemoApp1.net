using Application.Contracts.Model;
using Domain.Model;
using System.Linq.Expressions;

namespace Application.Contracts.Mappers;

public static class TodoItemMapper
{
    public static TodoItemDto ToDto(this TodoItem item) =>
        new(item.Id, item.Name, item.Status, item.SecureRandom, item.SecureDeterministic);

    public static TodoItem ToEntity(this TodoItemDto dto) =>
        new(dto.Name, dto.Status, dto.SecureRandom, dto.SecureDeterministic)
        {
            Id = dto.Id ?? Guid.Empty
        };

    public static readonly Expression<Func<TodoItem, TodoItemDto>> Projector = static item => new TodoItemDto(item.Id, item.Name, item.Status, item.SecureRandom, item.SecureDeterministic);
}

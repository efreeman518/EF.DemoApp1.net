using Application.Contracts.Model;
using Domain.Shared.Enums;

namespace SampleApp.Grpc.Mappers;
public static class TodoItemMapper
{
    //app -> grpc proto
    public static Proto.TodoItemDto ToGrpcDto(this TodoItemDto item)
    {
        return new Proto.TodoItemDto
        {
            Id = new Proto.NullableString { Isnull = item.Id == null, Data = (item.Id ?? Guid.Empty).ToString() },
            Name = item.Name, //?? default
            Status = Enum.Parse<Proto.TodoItemStatus>(item.Status.ToString()),
            Securerandom = new Proto.NullableString { Isnull = item.SecureRandom == null, Data = item.SecureRandom },
            Securedeterministic = new Proto.NullableString { Isnull = item.SecureDeterministic == null, Data = item.SecureDeterministic }
        };
    }

    //grpc proto -> app
    public static TodoItemDto ToAppDto(this Proto.TodoItemDto item)
    {
        return new TodoItemDto(
            item.Id.Isnull ? null : new Guid(item.Id.Data),
            item.Name,
            Enum.Parse<TodoItemStatus>(item.Status.ToString()),
            item.Securerandom.Isnull ? null : item.Securerandom.Data,
            item.Securedeterministic.Isnull ? null : item.Securedeterministic.Data
        );
    }
}

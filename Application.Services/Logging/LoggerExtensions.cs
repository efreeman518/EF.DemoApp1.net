using Package.Infrastructure.Common.Extensions;

namespace Application.Services.Logging;
public static class LoggerExtensions
{
    //Performant Logging
    //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    private static readonly Action<ILogger, Guid, string, string, Exception> _todoItemAddedLog =
        LoggerMessage.Define<Guid, string, string>(LogLevel.Information, new EventId(1, nameof(TodoItemAddedLog)), "TodoItem added: {Id} {Name} {TodoItem}");

    public static void TodoItemAddedLog(this ILogger logger, TodoItem item)
    {
        _todoItemAddedLog(logger, item.Id, item.Name, item.SerializeToJson()!, default!);
    }
}

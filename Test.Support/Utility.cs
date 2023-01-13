using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace Test.Support;
public static class Utility
{
    public static IConfigurationBuilder BuildConfiguration(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();
        return builder;
    }

    public static void SeedDefaultEntityData(TodoDbContextBase db, bool clear = true)
    {
        if (clear) db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
        db.Set<TodoItem>().AddRange(new List<TodoItem>
        {
                new TodoItem ("item1", TodoItemStatus.Created),
                new TodoItem ("item2", TodoItemStatus.InProgress),
                new TodoItem ("item3", TodoItemStatus.Completed)
        });
    }
}

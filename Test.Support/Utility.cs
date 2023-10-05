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
                new("item1a", TodoItemStatus.Created),
                new("item2a", TodoItemStatus.InProgress),
                new("item3a", TodoItemStatus.Completed)
        });
    }
}

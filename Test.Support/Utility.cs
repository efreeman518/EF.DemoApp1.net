using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Test.Support;

public static class Utility
{
    public static IConfigurationBuilder BuildConfiguration(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        //order matters here (last wins)
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();

        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Development")!;
        builder.AddJsonFile($"appsettings.{env}.json", true);

        //var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        //if (path != null) builder.AddJsonFile(path);
        //if (includeEnvironmentVars) builder.AddEnvironmentVariables();
        return builder;
    }

    public static void SeedDefaultEntityData(TodoDbContextBase db, bool clear = true)
    {
        if (clear) db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
        db.Set<TodoItem>().AddRange(new List<TodoItem>
        {
                new("item1a", TodoItemStatus.Created) { CreatedBy = "Test.Unit" },
                new("item2a", TodoItemStatus.InProgress) { CreatedBy = "Test.Unit" },
                new("item3a", TodoItemStatus.Completed){ CreatedBy = "Test.Unit" }
        });
    }

    public static async Task SeedRawSql(TodoDbContextBase db, List<string> pathToSQL)
    {
        // Run seed scripts
        pathToSQL.ForEach(async path =>
        {
            string[] files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), "*.sql");
            Array.Sort(files);
            foreach (var file in files)
            {
                Debug.Assert(true, "Attempt to seed with file {0}.", file);
                var sql = File.ReadAllText(file);
                await db.Database.ExecuteSqlRawAsync(sql);
            }
        });

        await db.SaveChangesAsync();
    }

    public static List<TodoItem> TodoItemListFactory(int size, TodoItemStatus? status = null)
    {
        var list = new List<TodoItem>();
        for (int i = 0; i < size; i++)
        {
            list.Add(TodoItemFactory($"a some entity {i}", status ?? RandomEnumValue<TodoItemStatus>()));
        }
        return list;
    }

    public static TodoItem TodoItemFactory(string name, TodoItemStatus? status = null, DateTime? createdDate = null)
    {
        return new TodoItem(name, status ?? RandomEnumValue<TodoItemStatus>()) { CreatedBy = "Test.Unit", CreatedDate = createdDate ?? DateTime.UtcNow };
    }

    private static readonly Random _R = new();
    private static TEnum? RandomEnumValue<TEnum>()
    {
        var v = Enum.GetValues(typeof(TEnum));
        return (TEnum?)v.GetValue(_R.Next(v.Length));
    }

}

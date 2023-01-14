using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Test.Support;
public class InMemoryDbBuilder
{
    //private static readonly object _lock = new();
    //private static readonly ConcurrentDictionary<string, DbContext> _contexts = new();

    //fluent config
    private bool _seedDefaultEntityData = false;
    private List<TodoItem>? _entityData;

    /// <summary>
    /// Builds a new DbContext; 
    /// </summary>
    /// <typeparam name="T">Specify the type of DbContext</typeparam>
    /// <param name="dbName">Enables retrieving an existing DbContext</param>
    /// <returns></returns>
    public T Build<T>(string? dbName = null) where T : DbContext
    {
        dbName ??= Guid.NewGuid().ToString();
        //InMemoryDB is like static, not created with each new DbContext, stays in memory for subsequent DbContexts for a given name
        DbContextOptions<T> options = new DbContextOptionsBuilder<T>().UseInMemoryDatabase(dbName).Options;
        T dbContext = (Activator.CreateInstance(typeof(T), options) as T)!;

        //Context specific setup
        if (dbContext is TodoDbContextBase db)
        {
            SetupContextTrxn(db);
        }

        dbContext!.SaveChanges();

        return dbContext;

    }

    private void SetupContextTrxn(TodoDbContextBase db)
    {
        if (_seedDefaultEntityData)
        {
            db.TodoItems.RemoveRange(db.TodoItems);
            db.TodoItems.AddRange(CreateDefaultSeedEntityData());
        }

        if (_entityData != null)
        {
            db.TodoItems.RemoveRange(db.TodoItems);
            db.TodoItems.AddRange(_entityData);
        }
    }

    public InMemoryDbBuilder SeedDefaultEntityData()
    {
        _seedDefaultEntityData = true;
        return this;
    }

    public InMemoryDbBuilder UseEntityData(Action<List<TodoItem>> action)
    {
        _entityData = new List<TodoItem>();
        action(_entityData);
        return this;
    }

    private static List<TodoItem> CreateDefaultSeedEntityData()
    {
        return new List<TodoItem>
        {
            new TodoItem ("item1a", TodoItemStatus.Created),
            new TodoItem ("item2a", TodoItemStatus.InProgress),
            new TodoItem ("item3a", TodoItemStatus.Completed)
        };
    }
}

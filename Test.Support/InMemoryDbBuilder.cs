using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Test.Support;
public class InMemoryDbBuilder
{
    private static readonly object _lock = new();
    private static readonly ConcurrentDictionary<string, DbContext> _contexts = new();

    //fluent config
    private bool _seedDefaultEntityData = false;
    private List<TodoItem>? _entityData;

    /// <summary>
    /// Gets an existing DbContext by name from cache or builds a new DbContext; 
    /// this enables the same db for setting up a repo and subsequently updating/investigating the db in the test code.
    /// </summary>
    /// <typeparam name="T">Specify the type of DbContext</typeparam>
    /// <param name="dbName">Enables retrieving an existing DbContext</param>
    /// <returns></returns>
    public T GetOrBuild<T>(string? dbName = null) where T : DbContext
    {
        dbName ??= Guid.NewGuid().ToString();
        if (!_contexts.TryGetValue(dbName, out DbContext? dbContext))
        {
            lock (_lock)
            {
                //in critical section, so check cache again
                if (!_contexts.TryGetValue(dbName, out dbContext))
                {
                    //InMemoryDB is like static, not created with each new DbContext, stays in memory for subsequent DbContexts for a given name
                    DbContextOptions<T> options = new DbContextOptionsBuilder<T>().UseInMemoryDatabase(dbName).Options;
                    dbContext = Activator.CreateInstance(typeof(T), options) as T;
                    _contexts.TryAdd(dbName, dbContext!);
                }
            }
        }

        //Context specific setup
        if (dbContext is TodoDbContextBase db)
        {
            SetupContextTrxn(db);
        }

        dbContext!.SaveChanges();

        return (T)dbContext;

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
                new TodoItem { Id = new Guid("7c15117d-db78-4c2f-8390-7f9bfda60a6e"), Name = "item1", IsComplete = false, Status = TodoItemStatus.Created, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow },
                new TodoItem { Id = new Guid("8c15117d-db78-4c2f-8390-7f9bfda60a61"), Name = "item2", IsComplete = false, Status = TodoItemStatus.InProgress, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow },
                new TodoItem { Id = new Guid("9c15117d-db78-4c2f-8390-7f9bfda60a63"), Name = "item3", IsComplete = true, Status = TodoItemStatus.Completed, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow }
        };
    }
}

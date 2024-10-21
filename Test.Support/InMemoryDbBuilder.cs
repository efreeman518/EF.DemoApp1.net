using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Test.Support;
public class InMemoryDbBuilder
{
    //fluent config
    private bool _seedDefaultEntityData = false;
    private List<TodoItem>? _entityData;

    /// <summary>
    /// Builds a new DbContext; 
    /// </summary>
    /// <typeparam name="T">Specify the type of DbContext</typeparam>
    /// <param name="dbName">Enables retrieving an existing DbContext</param>
    /// <returns></returns>
    public T BuildInMemory<T>(string? dbName = null) where T : DbContext
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

    public T BuildSQLite<T>() where T : DbContext
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<T>().UseSqlite(connection).Options;
        T dbContext = (Activator.CreateInstance(typeof(T), options) as T)!;

        if (dbContext != null)
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

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
            //db.TodoItems.RemoveRange(db.TodoItems);
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
        _entityData = [];
        action(_entityData);
        return this;
    }

    private static List<TodoItem> CreateDefaultSeedEntityData()
    {
        //RowVersion value required for SQLite insert = 'NOT NULL constraint failed: RowVersion'
        var list = new List<TodoItem>
        {
            new("item1a", TodoItemStatus.Created),
            new("item2a", TodoItemStatus.InProgress),
            new("item3a", TodoItemStatus.Completed)
        };
        return list;
    }

}

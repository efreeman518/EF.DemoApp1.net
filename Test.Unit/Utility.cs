using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.Unit;

public static class Utility
{
    #region InMemory DbContext

    //InMemory is like static, not created with each new DbContext, stays in memory for subsequent DbContexts for a given name
    public static TodoContext SetupInMemoryDB(string uniqueName, bool seed)
    {
        DbContextOptions<TodoContext> options = new DbContextOptionsBuilder<TodoContext>().UseInMemoryDatabase(uniqueName).Options;
        TodoContext db = new(options);
        if (seed) SeedInMemoryDB(db);
        return db;
    }

    public static void SeedInMemoryDB(TodoContext db)
    {
        db.TodoItems.AddRange(GetSeedData());
        db.SaveChanges();
    }

    public static void ReseedInMemoryDB(TodoContext db)
    {
        db.TodoItems.RemoveRange(db.TodoItems);
        SeedInMemoryDB(db);
    }

    public static List<TodoItem> GetSeedData()
    {
        return new List<TodoItem>
            {
                 new TodoItem { Id = new Guid("7c15117d-db78-4c2f-8390-7f9bfda60a6e"), Name = "item1", IsComplete = false, Status = TodoItemStatus.Created, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow },
                 new TodoItem { Id = new Guid("8c15117d-db78-4c2f-8390-7f9bfda60a61"), Name = "item2", IsComplete = false, Status = TodoItemStatus.InProgress, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow },
                 new TodoItem { Id = new Guid("9c15117d-db78-4c2f-8390-7f9bfda60a63"), Name = "item3", IsComplete = true, Status = TodoItemStatus.Completed, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow }
            };
    }

    public static void UpsertInMemoryDB(TodoContext db)
    {
        Upsert(db, new TodoItem { Id = new Guid("7c15117d-db78-4c2f-8390-7f9bfda60a6e"), Name = "item1", IsComplete = false, Status = TodoItemStatus.Created, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow });
        Upsert(db, new TodoItem { Id = new Guid("8c15117d-db78-4c2f-8390-7f9bfda60a61"), Name = "item2", IsComplete = false, Status = TodoItemStatus.InProgress, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow });
        Upsert(db, new TodoItem { Id = new Guid("9c15117d-db78-4c2f-8390-7f9bfda60a63"), Name = "item3", IsComplete = true, Status = TodoItemStatus.Completed, CreatedBy = "UnitTest", UpdatedBy = "UnitTest", CreatedDate = DateTime.UtcNow });
        db.SaveChanges();
    }

    private static void Upsert(TodoContext db, TodoItem item)
    {
        if (!db.Set<TodoItem>().Any(td => td.Id == item.Id)) db.Add(item);
    }

    #endregion
}

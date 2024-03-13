using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;

namespace Test.Support;

public static class TodoDbContextSupport
{
    public static void ClearEntityData(this TodoDbContextBase db)
    {
        db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
    }

    public static void SeedEntityData(this TodoDbContextBase db, int size = 10, TodoItemStatus? status = null)
    {
        db.Set<TodoItem>().AddRange(TodoItemListFactory(size, status));
    }

    public static List<TodoItem> TodoItemListFactory(int size = 10, TodoItemStatus? status = null)
    {
        var list = new List<TodoItem>();
        for (int i = 0; i < size; i++)
        {
            list.Add(TodoItemFactory($"a-{Utility.RandomString(10)}", status ?? DbSupport.RandomEnumValue<TodoItemStatus>()));
        }
        return list;
    }

    public static TodoItem TodoItemFactory(string name, TodoItemStatus? status = null, DateTime? createdDate = null)
    {
        return new TodoItem(name, status ?? DbSupport.RandomEnumValue<TodoItemStatus>()) { CreatedBy = "Test.Unit", CreatedDate = createdDate ?? DateTime.UtcNow };
    }
}

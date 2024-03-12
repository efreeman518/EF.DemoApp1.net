using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;

namespace Test.Support;
public static class TodoDbContextSupport
{
    public static void SeedEntityData(this TodoDbContextBase db, bool clear = true, int size = 10, TodoItemStatus? status = null)
    {
        if (clear) db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
        db.Set<TodoItem>().AddRange(TodoItemListFactory(size, status));
    }

    public static List<TodoItem> TodoItemListFactory(int size = 10, TodoItemStatus? status = null)
    {
        var list = new List<TodoItem>();
        for (int i = 0; i < size; i++)
        {
            list.Add(TodoItemFactory($"a some entity {i}", status ?? DbSupport.RandomEnumValue<TodoItemStatus>()));
        }
        return list;
    }

    public static TodoItem TodoItemFactory(string name, TodoItemStatus? status = null, DateTime? createdDate = null)
    {
        return new TodoItem(name, status ?? DbSupport.RandomEnumValue<TodoItemStatus>()) { CreatedBy = "Test.Unit", CreatedDate = createdDate ?? DateTime.UtcNow };
    }
}

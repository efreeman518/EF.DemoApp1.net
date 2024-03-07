using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;

namespace Test.Support;
public static class TodoDbContextSupport
{
    public static void SeedDefaultEntityData(this TodoDbContextBase db, bool clear = true)
    {
        if (clear) db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
        db.Set<TodoItem>().AddRange(new List<TodoItem>
        {
            new("item1a", TodoItemStatus.Created) { CreatedBy = "Test.Unit" },
            new("item2a", TodoItemStatus.InProgress) { CreatedBy = "Test.Unit" },
            new("item3a", TodoItemStatus.Completed){ CreatedBy = "Test.Unit" }
        });
    }

    public static List<TodoItem> TodoItemListFactory(int size, TodoItemStatus? status = null)
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

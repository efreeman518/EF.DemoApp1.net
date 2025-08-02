namespace Package.Infrastructure.Common.Contracts;

public record StaticItem<TId, TValue>(TId Id, string Name, TValue? Value = default);

public record StaticList<T>(IReadOnlyList<T> Items);

/// <summary>
/// Duplicated from the Application.Models.Static namespace to avoid that dependency in the UI project.
/// A simple container for collections of static data lists and individual values.
/// </summary>
public record StaticData
{
    /// <summary>
    /// Stores lists of static items, keyed by the list name.
    /// The value is stored as 'object' to accommodate any 'StaticList<T>' type.
    /// </summary>
    public Dictionary<string, object> Lists { get; set; } = [];

    /// <summary>
    /// Stores individual static items, keyed by a unique name.
    /// The value is stored as 'object' to accommodate any 'StaticItem<TId, TValue>' type.
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = [];

    /// <summary>
    /// Adds a 'StaticList<T>' to the 'Lists' dictionary with a specified key.
    /// </summary>
    public void AddList<T>(string listName, StaticList<T> list)
    {
        Lists[listName] = list;
    }

    /// <summary>
    /// Retrieves a 'StaticList<T>' from the 'Lists' dictionary in a type-safe way.
    /// </summary>
    /// <returns>The typed list if found and the type matches, otherwise null.</returns>
    public StaticList<T>? GetList<T>(string listName)
    {
        if (Lists.TryGetValue(listName, out var listObject) && listObject is StaticList<T> typedList)
        {
            return typedList;
        }
        return null;
    }

    /// <summary>
    /// Adds a 'StaticItem<TId, TValue>' to the 'Values' dictionary.
    /// The item's 'Name' property is used as the key.
    /// </summary>
    public void AddValue<TId, TValue>(StaticItem<TId, TValue> item)
    {
        Values[item.Name] = item;
    }

    /// <summary>
    /// Retrieves a 'StaticItem<TId, TValue>' from the 'Values' dictionary in a type-safe way.
    /// </summary>
    /// <returns>The typed item if found and the type matches, otherwise null.</returns>
    public StaticItem<TId, TValue>? GetValue<TId, TValue>(string key)
    {
        if (Values.TryGetValue(key, out var valueObject) && valueObject is StaticItem<TId, TValue> typedItem)
        {
            return typedItem;
        }
        return null;
    }
}

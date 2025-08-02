using System.Text.Json.Serialization;

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
    [JsonPropertyName("lists")]
    public Dictionary<string, object> Lists { get; } = [];

    /// <summary>
    /// Stores individual static items, keyed by a unique name.
    /// The value is stored as 'object' to accommodate any 'StaticItem<TId, TValue>' type.
    /// </summary>
    [JsonPropertyName("values")]
    public Dictionary<string, object> Values { get; } = [];

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

        // Handle JsonElement case - when JSON deserialization stores the object as JsonElement
        if (listObject is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                var deserializedList = System.Text.Json.JsonSerializer.Deserialize<StaticList<T>>(jsonElement.GetRawText());
                if (deserializedList != null)
                {
                    // Cache the properly typed object for future use
                    Lists[listName] = deserializedList;
                    return deserializedList;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Deserialization failed, return null
            }
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

        // Handle JsonElement case - when JSON deserialization stores the object as JsonElement
        if (valueObject is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                var deserializedItem = System.Text.Json.JsonSerializer.Deserialize<StaticItem<TId, TValue>>(jsonElement.GetRawText());
                if (deserializedItem != null)
                {
                    // Cache the properly typed object for future use
                    Values[key] = deserializedItem;
                    return deserializedItem;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Deserialization failed, return null
            }
        }

        return null;
    }
}

using System.Text.Json;

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

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    /// <summary>
    /// Retrieves a 'StaticList<T>' from the 'Lists' dictionary in a type-safe way.
    /// </summary>
    /// <returns>The typed list if found and the type matches, otherwise null.</returns>
    public StaticList<T>? GetList<T>(string listName)
    {
        if (!Lists.TryGetValue(listName, out var listObject))
        {
            return null;
        }

        // Direct cast works if object was manually added
        if (listObject is StaticList<T> typedList)
        {
            return typedList;
        }

        // Handle JsonElement from deserialization
        if (listObject is JsonElement jsonElement)
        {
            try
            {
                var result = JsonSerializer.Deserialize<StaticList<T>>(jsonElement.GetRawText(), _jsonSerializerOptions);
                if (result != null)
                {
                    Lists[listName] = result; // Cache for next time
                    return result;
                }
            }
            catch (JsonException)
            {
                // If that fails, try to get just the items array
                try
                {
                    if (jsonElement.TryGetProperty("items", out var itemsElement))
                    {
                        JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
                        var options = jsonSerializerOptions;
                        var items = JsonSerializer.Deserialize<IReadOnlyList<T>>(itemsElement.GetRawText(), options);
                        if (items != null)
                        {
                            var staticList = new StaticList<T>(items);
                            Lists[listName] = staticList;
                            return staticList;
                        }
                    }
                }
                catch (JsonException) { /* Fall through to return null */ }
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
        if (!Values.TryGetValue(key, out var valueObject))
        {
            return null;
        }

        // Direct cast works if object was manually added
        if (valueObject is StaticItem<TId, TValue> typedItem)
        {
            return typedItem;
        }

        // Handle JsonElement from deserialization
        if (valueObject is JsonElement jsonElement)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<StaticItem<TId, TValue>>(jsonElement.GetRawText(), options);
                if (result != null)
                {
                    Values[key] = result; // Cache for next time
                    return result;
                }
            }
            catch (JsonException)
            {
                // If that fails, try to manually construct from properties
                try
                {
                    if (jsonElement.TryGetProperty("id", out var idElement) &&
                        jsonElement.TryGetProperty("name", out var nameElement))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var id = JsonSerializer.Deserialize<TId>(idElement.GetRawText(), options);
                        var name = nameElement.GetString();

                        TValue? value = default;
                        if (jsonElement.TryGetProperty("value", out var valueElement))
                        {
                            value = JsonSerializer.Deserialize<TValue>(valueElement.GetRawText(), options);
                        }

                        if (!EqualityComparer<TId>.Default.Equals(id, default!) && name != null)
                        {
                            // Replace this block inside GetValue<TId, TValue> method
                            if (!EqualityComparer<TId>.Default.Equals(id, default!) && name != null)
                            {
                                var staticItem1 = new StaticItem<TId, TValue>(id!, name, value);
                                Values[key] = staticItem1;
                                return staticItem1;
                            }
                            var staticItem = new StaticItem<TId, TValue>(id!, name!, value);
                            Values[key] = staticItem;
                            return staticItem;
                        }
                    }
                }
                catch (JsonException) { /* Fall through to return null */ }
            }
        }

        return null;
    }
}

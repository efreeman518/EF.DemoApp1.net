using System.Text.Json;

namespace Package.Infrastructure.Common.Contracts;

public record StaticItem<TId, TValue>(TId? Id, string? Name, TValue? Value = default);

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
    /// The property is get-only to prevent reassignment while allowing JSON population and internal mutation.
    /// </summary>
    public Dictionary<string, object> Lists { get; } = [];

    /// <summary>
    /// Stores individual static items, keyed by a unique name.
    /// The value is stored as 'object' to accommodate any value type 'TValue'.
    /// The property is get-only to prevent reassignment while allowing JSON population and internal mutation.
    /// </summary>
    public Dictionary<string, object> Values { get; } = [];

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
    /// Adds to the Values dictionary (or removes if value is null)
    /// </summary>
    public void AddValue<TValue>(string key, TValue? val)
    {
        // Ensure that null values are not assigned to the dictionary to avoid CS8601
        if (val is not null)
        {
            Values[key] = val!;
        }
        else
        {
            Values.Remove(key);
        }
    }

    /// <summary>
    /// Retrieves a value from the 'Values' dictionary in a type-safe way.
    /// </summary>
    /// <returns>
    /// The typed value if found and convertible, otherwise null.
    /// Note: The method returns TValue? so for value types (e.g., int) the return type is Nullable<T> (e.g., int?).
    /// </returns>
    public TValue? GetValue<TValue>(string key)
    {
        if (!Values.TryGetValue(key, out var valueObject))
        {
            // Missing key -> return null (also for value types because return type is Nullable<TValue>)
            return default;
        }

        // Direct cast works if object was manually added
        if (valueObject is TValue typedValue)
        {
            return typedValue;
        }

        // Handle JsonElement from deserialization of StaticData
        if (valueObject is JsonElement jsonElement)
        {
            try
            {
                if (jsonElement.ValueKind == JsonValueKind.Null)
                {
                    return default; // null
                }

                var result = JsonSerializer.Deserialize<TValue>(jsonElement.GetRawText(), _jsonSerializerOptions);

                // Cache successful materialization to avoid re-deserialization next time.
                // For value types, result may be default(T), which is valid and should be cached.
                if (result is not null || typeof(TValue).IsValueType)
                {
                    Values[key] = result!;
                    return result;
                }
            }
            catch (JsonException)
            {
                // Best-effort fallbacks for primitive conversions
                try
                {
                    object? fallback = jsonElement.ValueKind switch
                    {
                        JsonValueKind.String when typeof(TValue) == typeof(string) => jsonElement.GetString(),
                        JsonValueKind.Number when typeof(TValue) == typeof(int) && jsonElement.TryGetInt32(out var i) => i,
                        JsonValueKind.Number when typeof(TValue) == typeof(long) && jsonElement.TryGetInt64(out var l) => l,
                        JsonValueKind.Number when typeof(TValue) == typeof(double) && jsonElement.TryGetDouble(out var d) => d,
                        JsonValueKind.True when typeof(TValue) == typeof(bool) => true,
                        JsonValueKind.False when typeof(TValue) == typeof(bool) => false,
                        _ => null
                    };

                    if (fallback is not null)
                    {
                        Values[key] = (TValue)fallback;
                        return (TValue)fallback;
                    }
                }
                catch
                {
                    // Fall through to return null
                }
            }
        }

        // Type mismatch or unsupported stored type -> treat as not found
        return default; // null
    }
}

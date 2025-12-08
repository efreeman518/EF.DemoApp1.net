using System.Text.Json;

namespace Package.Infrastructure.Common.Extensions;

/// <summary>
/// Extension methods for System.Text.Json.JsonElement providing type-safe conversions and navigation.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Converts a JsonElement to a string representation based on its ValueKind.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>A string representation of the JSON value, or empty string for null/undefined</returns>
    public static string ToSafeString(this JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => element.GetRawText()
    };

    /// <summary>
    /// Converts a JsonElement to a string, returning a default value if unsuccessful.
    /// </summary>
    public static string ToSafeString(this JsonElement element, string defaultValue)
    {
        try
        {
            return element.ToSafeString();
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Converts a JsonElement to an appropriate .NET object with type inference.
    /// </summary>
    public static object? ToObject(this JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt32(out var i) ? i :
                               element.TryGetInt64(out var l) ? l :
                               element.GetDecimal(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(e => e.ToObject()).ToList(),
        JsonValueKind.Object => element.ToStronglyTypedDictionary(),
        _ => element.ToString()
    };

    /// <summary>
    /// Converts a JsonElement object to a dictionary with case-insensitive keys and string values.
    /// </summary>
    public static Dictionary<string, string> ToDictionary(this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return [];

        return element.EnumerateObject()
            .ToDictionary(
                p => p.Name,
                p => p.Value.ValueKind == JsonValueKind.String
                    ? p.Value.GetString() ?? string.Empty
                    : p.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a JsonElement object to a dictionary with strongly-typed values.
    /// </summary>
    public static Dictionary<string, object?> ToStronglyTypedDictionary(this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return [];

        return element.EnumerateObject()
            .ToDictionary(
                p => p.Name,
                p => p.Value.ToObject(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to get a boolean value from a JsonElement.
    /// </summary>
    public static bool TryGetBool(this JsonElement element, out bool value)
    {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = element.GetBoolean();
            return true;
        }
        value = false;
        return false;
    }

    /// <summary>
    /// Tries to get an integer value from a JsonElement.
    /// </summary>
    public static bool TryGetInt(this JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
            return true;
        value = 0;
        return false;
    }

    /// <summary>
    /// Tries to get a long value from a JsonElement.
    /// </summary>
    public static bool TryGetLong(this JsonElement element, out long value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out value))
            return true;
        value = 0;
        return false;
    }

    /// <summary>
    /// Tries to get a decimal value from a JsonElement.
    /// </summary>
    public static bool TryGetDecimal(this JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            value = element.GetDecimal();
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>
    /// Tries to get a double value from a JsonElement.
    /// </summary>
    public static bool TryGetDouble(this JsonElement element, out double value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value))
            return true;
        value = 0;
        return false;
    }

    /// <summary>
    /// Gets a string value or null if not a string.
    /// </summary>
    public static string? GetStringOrNull(this JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    /// <summary>
    /// Gets a list of strings from a JsonElement array.
    /// </summary>
    public static List<string> ToStringList(this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return [];

        return [.. element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)];
    }

    /// <summary>
    /// Gets a list of objects from a JsonElement array.
    /// </summary>
    public static List<object?> ToObjectList(this JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return [];

        return [.. element.EnumerateArray().Select(item => item.ToObject())];
    }

    /// <summary>
    /// Navigates through nested properties in a JsonElement.
    /// </summary>
    public static bool TryGetNestedProperty(this JsonElement element, string[] propertyPath, out JsonElement result)
    {
        result = default;
        if (propertyPath.Length == 0) return false;

        var current = element;
        foreach (var prop in propertyPath)
        {
            if (!current.TryGetProperty(prop, out current))
                return false;
        }
        result = current;
        return true;
    }

    /// <summary>
    /// Gets a nested property value as a string.
    /// </summary>
    public static string? GetNestedString(this JsonElement element, params string[] propertyPath)
    {
        if (!element.TryGetNestedProperty(propertyPath, out var result))
            return null;
        return result.GetStringOrNull() ?? result.ToString();
    }

    /// <summary>
    /// Gets a nested property value as an object.
    /// </summary>
    public static object? GetNestedObject(this JsonElement element, params string[] propertyPath)
    {
        if (!element.TryGetNestedProperty(propertyPath, out var result))
            return null;
        return result.ToObject();
    }

    /// <summary>
    /// Tries to deserialize a JsonElement to a specific type.
    /// </summary>
    public static T? Deserialize<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Checks if the element has a property with the specified name (case-insensitive).
    /// </summary>
    public static bool HasProperty(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;

        return element.EnumerateObject()
            .Any(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets property value with case-insensitive name matching.
    /// </summary>
    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object) return false;

        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        return false;
    }
}
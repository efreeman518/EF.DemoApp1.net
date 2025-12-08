using Package.Infrastructure.Common.Extensions;
using System.Text.Json;

namespace Package.Infrastructure.Common;

/// <summary>
/// Utility methods for working with JSON strings.
/// </summary>
public static class JsonUtility
{
    private static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions { WriteIndented = true };

    /// <summary>
    /// Gets a typed field value from a JSON string.
    /// </summary>
    public static T? GetField<T>(string? json, string field) where T : struct
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(field, out var el)) return null;

            return typeof(T) switch
            {
                Type t when t == typeof(int) && el.TryGetInt(out var i) => (T)(object)i,
                Type t when t == typeof(long) && el.TryGetLong(out var l) => (T)(object)l,
                Type t when t == typeof(decimal) && el.TryGetDecimal(out var d) => (T)(object)d,
                Type t when t == typeof(double) && el.TryGetDouble(out var db) => (T)(object)db,
                Type t when t == typeof(bool) && el.TryGetBool(out var b) => (T)(object)b,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a string field value from a JSON string.
    /// </summary>
    public static string? GetStringField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(field, out var el) ? el.GetStringOrNull() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a string array field value from a JSON string.
    /// </summary>
    public static List<string>? GetArrayField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Array)
                return el.ToStringList();
        }
        catch
        {
            // handle
        }
        return null;
    }

    /// <summary>
    /// Gets a nested field value as a string.
    /// </summary>
    public static string? GetNestedField(string? json, params string[] fieldPath)
    {
        if (string.IsNullOrWhiteSpace(json) || fieldPath.Length == 0) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetNestedString(fieldPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a nested field value as an object.
    /// </summary>
    public static object? GetNestedObject(string? json, params string[] fieldPath)
    {
        if (string.IsNullOrWhiteSpace(json) || fieldPath.Length == 0) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetNestedObject(fieldPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    public static T? Deserialize<T>(string? json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    public static string? Serialize<T>(T? obj, JsonSerializerOptions? options = null)
    {
        if (obj is null) return null;
        try
        {
            return JsonSerializer.Serialize(obj, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Tries to parse a JSON string into a JsonDocument.
    /// </summary>
    public static bool TryParse(string? json, out JsonDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            document = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Merges two JSON objects, with the second object properties overriding the first.
    /// </summary>
    public static string? MergeJson(string? json1, string? json2)
    {
        if (string.IsNullOrWhiteSpace(json1)) return json2;
        if (string.IsNullOrWhiteSpace(json2)) return json1;

        try
        {
            using var doc1 = JsonDocument.Parse(json1);
            using var doc2 = JsonDocument.Parse(json2);

            if (doc1.RootElement.ValueKind != JsonValueKind.Object ||
                doc2.RootElement.ValueKind != JsonValueKind.Object)
                return json1;

            var merged = doc1.RootElement.ToStronglyTypedDictionary();

            foreach (var prop in doc2.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.ToObject();
            }

            return JsonSerializer.Serialize(merged);
        }
        catch
        {
            return json1;
        }
    }

    /// <summary>
    /// Extracts metadata as a dictionary from various object types.
    /// Supports IDictionary, JsonElement, and serializable objects.
    /// </summary>
    public static Dictionary<string, object?> ExtractMetadata(object? metadata)
    {
        if (metadata is null) return [];

        if (metadata is IDictionary<string, object?> direct)
            return new Dictionary<string, object?>(direct, StringComparer.OrdinalIgnoreCase);

        if (metadata is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return je.ToStronglyTypedDictionary();

        try
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(metadata));
            return doc.RootElement.ValueKind == JsonValueKind.Object
                ? doc.RootElement.ToStronglyTypedDictionary()
                : [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Converts a JSON string to a strongly-typed dictionary.
    /// </summary>
    public static Dictionary<string, object?>? ToDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                ? doc.RootElement.ToStronglyTypedDictionary()
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON.
    /// </summary>
    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pretty prints a JSON string with indentation.
    /// </summary>
    public static string? PrettyPrint(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, IndentedOptions);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Minifies a JSON string by removing whitespace.
    /// </summary>
    public static string? Minify(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            return json;
        }
    }
}

using System.Text.Json;

namespace Package.Infrastructure.Common.Extensions;

/// <summary>
/// Extension methods for System.Text.Json.JsonElement to provide safe string conversions.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Converts a JsonElement to a string representation based on its ValueKind.
    /// Handles strings, numbers, booleans, null, and other JSON types safely.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>A string representation of the JSON value, or empty string for null</returns>
    public static string ToSafeString(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => element.GetRawText(), // JSON array as string
            JsonValueKind.Object => element.GetRawText(), // JSON object as string
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Tries to convert a JsonElement to a string, returning a default value if unsuccessful.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <param name="defaultValue">The default value to return if conversion fails</param>
    /// <returns>The string representation or the default value</returns>
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
}
using System.Text.Json;

namespace Package.Infrastructure.Common;
public static class JsonUtility
{
    // Simple JSON field extractors
    public static string? GetJsonField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el))
                return el.GetString();
        }
        catch
        {
            // handle
        }
        return null;
    }

    public static int? GetJsonIntField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Array && el.GetArrayLength() > 0)
                return el[0].GetInt32();
        }
        catch
        {
            //handle
        }
        return null;
    }
}

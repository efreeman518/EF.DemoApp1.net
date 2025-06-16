using Refit;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;

public static class ApiExceptionExtensions
{
    // Cache the JsonSerializerOptions instance to avoid creating a new one for every operation
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parse ProblemDetails if returned to Refit
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static ProblemDetails? ToProblemDetails(this ApiException ex)
    {
        if (string.IsNullOrEmpty(ex.Content)) return null;

        return JsonSerializer.Deserialize<ProblemDetails>(ex.Content, CachedJsonSerializerOptions);
    }
}

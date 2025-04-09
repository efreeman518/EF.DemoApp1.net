using Refit;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;
public static class RefitCallHelper
{
    public static async Task<ApiResult<T>> TryApiCallAsync<T>(Func<Task<T>> apiCall)
    {
        try
        {
            var result = await apiCall();
            return ApiResult<T>.Success(result);
        }
        catch (ApiException ex)
        {
            var problem = DeserializeProblemDetails(ex.Content);
            return ApiResult<T>.Failure(problem ?? new ProblemDetails
            {
                Status = (int)ex.StatusCode,
                Title = "Unexpected error",
                Detail = "An unexpected error occurred."
            });
        }
    }

    private static ProblemDetails? DeserializeProblemDetails(string? content)
    {
        if (string.IsNullOrEmpty(content)) return null;

        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}

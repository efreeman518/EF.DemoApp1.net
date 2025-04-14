using Refit;
using System.Net;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;
public static class RefitCallHelper
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiResult<T>> TryApiCallAsync<T>(Func<Task<T>> apiCall)
    {
        try
        {
            var result = await apiCall();
            return ApiResult<T>.Success(result);
        }
        catch (ApiException ex)
        {
            return ApiResult<T>.Failure(DeserializeProblemDetails(ex.StatusCode, ex.Content));
        }
    }

    private static ProblemDetails DeserializeProblemDetails(HttpStatusCode statusCode, string? content)
    {
        if (string.IsNullOrEmpty(content)) return new ProblemDetails
        {
            Status = (int)statusCode,
            Title = "Unexpected empty response",
            Detail = "Response was empty or null."
        };

        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(content, DefaultJsonSerializerOptions)
                ?? new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = "Unexpected error",
                    Detail = "Failed to deserialize the error response."
                };
        }
        catch
        {
            return new ProblemDetails
            {
                Status = (int)statusCode,
                Title = "Response deserialization error",
                Detail = $"Failed to deserialize the error response: {content}"
            };
        }
    }
}

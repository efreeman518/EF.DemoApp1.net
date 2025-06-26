using Refit;
using System.Net;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;
public static class RefitCallHelper
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiResult<T>> TryApiCallAsync<T>(Func<Task<T>> apiCall, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a linked token with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Reasonable timeout

            var result = await Task.Run(async () => await apiCall().ConfigureAwait(false), cts.Token);
            return ApiResult<T>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return ApiResult<T>.Failure(new ProblemDetails
            {
                Status = (int)HttpStatusCode.GatewayTimeout,
                Title = "Request Timeout",
                Detail = "The request took too long to complete."
            });
        }
        catch (ApiException ex)
        {
            //return ApiResult<T>.Failure(DeserializeProblemDetails(ex.StatusCode, ex.Content));
            string errorMessage = "An error occurred.";

            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                errorMessage = "Not authorized.";
            }
            else if (!string.IsNullOrEmpty(ex.Content))
            {
                try
                {
                    // Attempt to parse the JSON content for more details
                    var errorDetails = JsonSerializer.Deserialize<ProblemDetails>(ex.Content);
                    if (errorDetails != null && !string.IsNullOrEmpty(errorDetails.Detail))
                    {
                        errorMessage = errorDetails.Detail;
                    }
                }
                catch (JsonException)
                {
                    errorMessage = "Failed to parse error details.";
                }
            }

            return ApiResult<T>.Failure(new ProblemDetails
            {
                Status = (int)ex.StatusCode,
                Detail = errorMessage
            });
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure(new ProblemDetails
            {
                Status = 500,
                Title = "Unexpected Error",
                Detail = ex.Message
            });
        }
    }

    public static async Task<ApiResult> TryApiCallVoidAsync(Func<Task> apiCall, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a linked token with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Reasonable timeout

            await Task.Run(async () => await apiCall().ConfigureAwait(false), cts.Token);
            return ApiResult.Success();
        }
        catch (OperationCanceledException)
        {
            return ApiResult.Failure(new ProblemDetails
            {
                Status = (int)HttpStatusCode.GatewayTimeout,
                Title = "Request Timeout",
                Detail = "The request took too long to complete."
            });
        }
        catch (ApiException ex)
        {
            return ApiResult.Failure(DeserializeProblemDetails(ex.StatusCode, ex.Content));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(new ProblemDetails
            {
                Status = 500,
                Title = "Unexpected Error",
                Detail = ex.Message
            });
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
                    Detail = $"Failed to deserialize the error response. {content ?? ""}"
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

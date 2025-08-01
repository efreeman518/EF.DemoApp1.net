﻿using Refit;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Package.Infrastructure.Utility.UI;
public static class RefitCallHelper
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiResult<T>> TryApiCallAsync<T>(Func<Task<T>> apiCall, bool failOnTimeout = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a linked token with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Reasonable timeout

            var result = await Task.Run(async () => await apiCall().ConfigureAwait(false), cts.Token);
            return ApiResult<T>.Success(result);
        }
        catch (OperationCanceledException) when (!failOnTimeout)
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
            return ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ApiResult<T>.Failure(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Not Authorized",
                    Detail = "You do not have permission to perform this action."
                }),
                HttpStatusCode.Forbidden => ApiResult<T>.Failure(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to perform this action."
                }),
                HttpStatusCode.NotFound => ApiResult<T>.Failure(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Not Found",
                    Detail = "Resource not found."
                }),
                HttpStatusCode.MethodNotAllowed => ApiResult<T>.Failure(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.MethodNotAllowed,
                    Title = "Method Not Allowed",
                    Detail = "Method Not Allowed."
                }),
                _ => ApiResult<T>.Failure(DeserializeProblemDetails(ex.StatusCode, ex.Content))
            };
        }
        catch (HttpRequestException httpEx) // Detect network errors
        {
            return ApiResult<T>.Failure(new ProblemDetails
            {
                Status = 503,
                Title = "API Unreachable",
                Detail = $"The API may be offline or unreachable. Please check your network connection or try again later. {httpEx.Message}"
            });
        }
        catch (SocketException sockEx) // Detect socket errors
        {
            return ApiResult<T>.Failure(new ProblemDetails
            {
                Status = 503,
                Title = "API Unreachable",
                Detail = $"The API may be offline or unreachable. Please check your network connection or try again later. {sockEx.Message}"
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

    public static async Task<ApiResult> TryApiCallVoidAsync(Func<Task> apiCall, bool failOnTimeout = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a linked token with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Reasonable timeout

            await Task.Run(async () => await apiCall().ConfigureAwait(false), cts.Token);
            return ApiResult.Success();
        }
        catch (OperationCanceledException) when (!failOnTimeout)
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
        catch (HttpRequestException httpEx) // Detect network errors
        {
            return ApiResult.Failure(new ProblemDetails
            {
                Status = 503,
                Title = "API Unreachable",
                Detail = $"The API may be offline or unreachable. Please check your network connection or try again later. {httpEx.Message}"
            });
        }
        catch (SocketException sockEx) // Detect socket errors
        {
            return ApiResult.Failure(new ProblemDetails
            {
                Status = 503,
                Title = "API Unreachable",
                Detail = $"The API may be offline or unreachable. Please check your network connection or try again later. {sockEx.Message}"
            });
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

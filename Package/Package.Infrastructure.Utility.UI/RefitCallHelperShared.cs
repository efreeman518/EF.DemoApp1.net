using Refit;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Utility.UI;

internal static class RefitCallHelperShared
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex ArrayIndexRegex = new(@"\[\d+\]", RegexOptions.Compiled);
    private static readonly Regex SpaceCamelCaseRegex = new(@"(\B[A-Z])", RegexOptions.Compiled);

    internal static ProblemDetails MapApiException(
        ApiException ex,
        string? operationName,
        string emptyResponseTitle,
        Action<AuthErrorInfo>? onAuthError = null,
        IReadOnlyCollection<string>? correlationHeaderNames = null,
        bool captureRawError = false,
        int maxRawPreview = 1024,
        bool includeValidationErrorShaping = true)
    {
        var deserialized = DeserializeProblemDetails(ex.StatusCode, ex.Content, emptyResponseTitle, includeValidationErrorShaping);

        if (IsAuthTokenRevoked(ex.Content))
        {
            onAuthError?.Invoke(new AuthErrorInfo(
                Error: GetJsonField(ex.Content, "error") ?? "error_not_identified",
                ErrorDescription: GetJsonField(ex.Content, "error_description"),
                ErrorCode: GetJsonIntField(ex.Content, "error_codes"),
                SubError: GetJsonField(ex.Content, "suberror"),
                Problem: deserialized));
        }

        var pd = deserialized.Title != emptyResponseTitle
            ? deserialized
            : ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ProblemDetails { Status = 401, Title = "Not Authorized", Detail = "You do not have permission to perform this action." },
                HttpStatusCode.Forbidden => new ProblemDetails { Status = 403, Title = "Forbidden", Detail = "You do not have permission to perform this action." },
                HttpStatusCode.NotFound => new ProblemDetails { Status = 404, Title = "Not Found", Detail = "Resource not found." },
                HttpStatusCode.MethodNotAllowed => new ProblemDetails { Status = 405, Title = "Method Not Allowed", Detail = "Method not allowed for this endpoint." },
                (HttpStatusCode)429 => new ProblemDetails { Status = 429, Title = "Too Many Requests", Detail = "Rate limit exceeded. Please retry later." },
                _ => new ProblemDetails { Status = (int)ex.StatusCode, Title = "API Error", Detail = $"Unexpected API error ({(int)ex.StatusCode})." }
            };

        if (correlationHeaderNames is not null && correlationHeaderNames.Count > 0 && ex.Headers is not null)
        {
            try
            {
                foreach (var headerName in correlationHeaderNames)
                {
                    if (!ex.Headers.TryGetValues(headerName, out var values))
                    {
                        continue;
                    }

                    var value = values.FirstOrDefault();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    pd.Extensions ??= new Dictionary<string, object>();
                    pd.Extensions["correlationId"] = value;
                    break;
                }
            }
            catch
            {
                // ignored by design
            }
        }

        if (captureRawError && !string.IsNullOrEmpty(ex.Content))
        {
            var preview = ex.Content.Length > maxRawPreview
                ? ex.Content[..maxRawPreview] + "...(truncated)"
                : ex.Content;

            pd.Extensions ??= new Dictionary<string, object>();
            if (!pd.Extensions.ContainsKey("raw"))
            {
                pd.Extensions["raw"] = preview;
            }
        }

        return AttachOperation(pd, operationName);
    }

    internal static ProblemDetails DeserializeProblemDetails(
        HttpStatusCode statusCode,
        string? content,
        string emptyResponseTitle,
        bool includeValidationErrorShaping = true)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new ProblemDetails
            {
                Status = (int)statusCode,
                Title = emptyResponseTitle,
                Detail = "Response was empty or null."
            };
        }

        try
        {
            if (includeValidationErrorShaping)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var validationErrors = new List<string>();
                    foreach (var errorProperty in errorsElement.EnumerateObject())
                    {
                        var fieldName = errorProperty.Name;
                        if (errorProperty.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var messageElement in errorProperty.Value.EnumerateArray())
                        {
                            var message = messageElement.GetString();
                            if (string.IsNullOrWhiteSpace(message))
                            {
                                continue;
                            }

                            validationErrors.Add($"{FormatFieldName(fieldName)}: {message}");
                        }
                    }

                    var validationProblem = JsonSerializer.Deserialize<ProblemDetails>(content, JsonOptions);
                    if (validationProblem is not null)
                    {
                        if (validationProblem.Status == 0)
                        {
                            validationProblem.Status = (int)statusCode;
                        }

                        if (validationErrors.Count > 0)
                        {
                            validationProblem.Detail = string.Join("; ", validationErrors);
                            validationProblem.Extensions ??= new Dictionary<string, object>();
                            validationProblem.Extensions["validationErrors"] = validationErrors;
                        }

                        return validationProblem;
                    }
                }
            }

            var pd = JsonSerializer.Deserialize<ProblemDetails>(content, JsonOptions);
            if (pd is null)
            {
                return new ProblemDetails
                {
                    Status = (int)statusCode,
                    Title = "Unexpected error",
                    Detail = $"Failed to deserialize the error response. {content}"
                };
            }

            if (pd.Status == 0)
            {
                pd.Status = (int)statusCode;
            }

            return pd;
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

    internal static string AggregateMessage(HttpRequestException ex) =>
        ex.InnerException is null ? ex.Message : $"{ex.Message} (Inner: {ex.InnerException.Message})";

    internal static ProblemDetails CreateTimeoutProblem(string? operationName) =>
        AttachOperation(new ProblemDetails
        {
            Status = (int)HttpStatusCode.GatewayTimeout,
            Title = operationName is null ? "Request Timeout" : $"{operationName} Timeout",
            Detail = "The request took too long to complete."
        }, operationName);

    internal static ProblemDetails CreateNetworkProblem(string message, string? operationName) =>
        AttachOperation(new ProblemDetails
        {
            Status = 503,
            Title = operationName is null ? "API Unreachable" : $"{operationName} Unreachable",
            Detail = $"The API may be offline or unreachable. {message}"
        }, operationName);

    internal static ProblemDetails CreateGenericProblem(string message, string? operationName) =>
        AttachOperation(new ProblemDetails
        {
            Status = 500,
            Title = operationName is null ? "Unexpected Error" : $"{operationName} Failed",
            Detail = message
        }, operationName);

    internal static ProblemDetails AttachOperation(ProblemDetails problemDetails, string? operationName)
    {
        if (operationName is null)
        {
            return problemDetails;
        }

        problemDetails.Extensions ??= new Dictionary<string, object>();
        if (!problemDetails.Extensions.ContainsKey("operation"))
        {
            problemDetails.Extensions["operation"] = operationName;
        }

        return problemDetails;
    }

    internal static bool IsAuthTokenRevoked(string? content)
    {
        if (content is null)
        {
            return false;
        }

        return content.Contains("AADSTS50173", StringComparison.Ordinal) ||
               content.Contains("\"error\":\"invalid_grant\"", StringComparison.Ordinal) ||
               content.Contains("\"suberror\":\"bad_token\"", StringComparison.Ordinal) ||
               content.Contains("\"error_codes\":[50173]", StringComparison.Ordinal);
    }

    internal static string? GetJsonField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(field, out var element)
                ? element.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    internal static int? GetJsonIntField(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var element) &&
                element.ValueKind == JsonValueKind.Array &&
                element.GetArrayLength() > 0)
            {
                return element[0].GetInt32();
            }
        }
        catch
        {
            // ignored by design
        }

        return null;
    }

    internal static bool IsWasmStreamingError(Exception ex)
    {
        if (ex is InvalidOperationException ioe &&
            (ioe.Message.Contains("synchronous reads", StringComparison.OrdinalIgnoreCase) ||
             ioe.Message.Contains("BrowserHttpReadStream", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (ex is AggregateException aggregate &&
            aggregate.InnerException is InvalidOperationException inner &&
            (inner.Message.Contains("synchronous reads", StringComparison.OrdinalIgnoreCase) ||
             inner.Message.Contains("net_http_synchronous_reads_not_supported", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return GetAllExceptionMessages(ex).Any(message =>
            message.Contains("synchronous reads", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("net_http_synchronous_reads_not_supported", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("BrowserHttpReadStream", StringComparison.OrdinalIgnoreCase));
    }

    internal static ProblemDetails CreateWasmStreamingErrorProblem(string? operationName, Exception? ex = null)
    {
        var pd = new ProblemDetails
        {
            Status = 500,
            Title = ".NET 10 WASM Configuration Required",
            Detail = "Add <WasmEnableStreamingResponse>false</WasmEnableStreamingResponse> to your Blazor WASM project file (.csproj) to fix this error.",
            Extensions = new Dictionary<string, object>
            {
                ["operation"] = operationName ?? "API Call"
            }
        };

        if (ex is not null)
        {
            pd.Extensions["documentation"] = "https://learn.microsoft.com/en-us/dotnet/core/compatibility/networking/10.0/default-http-streaming";

            if (ex is InvalidOperationException || ex is AggregateException)
            {
                pd.Extensions["errorDetails"] = string.Join(" | ", GetAllExceptionMessages(ex));
            }
        }

        return pd;
    }

    private static List<string> GetAllExceptionMessages(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return messages;
    }

    private static string FormatFieldName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return "Field";
        }

        var cleaned = ArrayIndexRegex.Replace(fieldName, string.Empty);
        var segments = cleaned.Split('.');
        var lastSegment = segments.Length > 0 ? segments[^1] : cleaned;

        if (lastSegment.Length <= 3)
        {
            return lastSegment;
        }

        return SpaceCamelCaseRegex.Replace(lastSegment, " $1");
    }
}

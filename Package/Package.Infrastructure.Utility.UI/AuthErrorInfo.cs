using Refit;

namespace Package.Infrastructure.Utility.UI;

public sealed record AuthErrorInfo(
    string Error,
    string? ErrorDescription,
    int? ErrorCode,
    string? SubError,
    ProblemDetails Problem
);
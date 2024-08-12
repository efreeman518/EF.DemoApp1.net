using FluentValidation.Results;

namespace Package.Infrastructure.AspNetCore;

public static class ValidationHelper
{
    public static ValidationFailureResponse ToResponse(this IEnumerable<ValidationFailure> failure)
    {
        return new ValidationFailureResponse
        {
            Errors = failure.Select(e => e.ErrorMessage)
        };
    }

    public static string ToMessage(this IEnumerable<ValidationFailure> failure)
    {
        return string.Join("; ", failure.Select(e => e.ErrorMessage));
    }
}

public class ValidationFailureResponse
{
    public IEnumerable<string> Errors { get; set; } = [];
}

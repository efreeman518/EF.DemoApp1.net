using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Domain.Contracts;

namespace Package.Infrastructure.Common.Extensions;

/// <summary>
/// Provides extension methods for converting between DomainResult and Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a DomainResult to a Result.
    /// </summary>
    /// <param name="domainResult">The domain result to convert.</param>
    /// <returns>A Result instance.</returns>
    public static Result ToResult(this DomainResult domainResult)
    {
        if (domainResult.IsSuccess)
        {
            return Result.Success();
        }

        var errors = domainResult.Errors.Select(e => e.Error).ToList();
        return Result.Failure(errors);
    }

    /// <summary>
    /// Converts a DomainResult<T> to a Result<T>.
    /// </summary>
    /// <typeparam name="T">The type of the value in the result.</typeparam>
    /// <param name="domainResult">The domain result to convert.</param>
    /// <returns>A Result<T> instance.</returns>
    public static Result<T> ToResult<T>(this DomainResult<T> domainResult)
    {
        if (domainResult.IsSuccess)
        {
            // Assuming a successful domain result always has a value.
            // If Value can be null for a success case, additional handling might be needed.
            return domainResult.Value is not null
                ? Result<T>.Success(domainResult.Value)
                : Result<T>.Failure("Successful domain result has a null value.");
        }

        var errors = domainResult.Errors.Select(e => e.Error).ToList();
        return Result<T>.Failure(errors);
    }
}

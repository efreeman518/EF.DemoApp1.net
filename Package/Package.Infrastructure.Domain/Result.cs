using System.Collections.ObjectModel;

namespace Package.Infrastructure.Domain;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    private static readonly DomainError[] s_emptyDomainErrors = [];

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Convenience property - Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Contains the errors if the operation failed; otherwise, an empty collection.
    /// </summary>
    public IReadOnlyCollection<DomainError> Errors { get; }

    /// <summary>
    /// Protected constructor to initialize a Result instance.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="errors">The collection of domain errors if the operation failed.</param>
    protected Result(bool isSuccess, IReadOnlyCollection<DomainError>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? s_emptyDomainErrors;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A Result instance representing success.</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(string error) => new(false, [DomainError.Create(error)]);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of errors describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(IReadOnlyCollection<DomainError> errors) => new(false, errors);

    /// <summary>
    /// Creates a failed result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(Exception exception) => new(false, [DomainError.Create(exception.Message)]);

    /// <summary>
    /// Combines multiple Result instances into one. If any result is a failure, the combined result is a failure with aggregated errors.
    /// </summary>
    /// <param name="results">The array of Result instances to combine.</param>
    /// <returns>A combined Result instance.</returns>
    public static Result Combine(params Result[] results)
    {
        var failedResults = results.Where(r => r.IsFailure).ToList();
        if (failedResults.Count == 0)
        {
            return Success();
        }

        var errors = failedResults.SelectMany(r => r.Errors).ToList();
        return Failure(new ReadOnlyCollection<DomainError>(errors));
    }

    /// <summary>
    /// Compares this Result instance with another for equality.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Result other) return false;
        return IsSuccess == other.IsSuccess && Errors.SequenceEqual(other.Errors);
    }

    /// <summary>
    /// Gets the hash code for this Result instance.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(IsSuccess);
        foreach (var error in Errors)
        {
            hashCode.Add(error);
        }
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Implicitly converts a Result instance to a boolean, returning true if the result is successful.
    /// </summary>
    /// <param name="result">The Result instance to convert.</param>
    public static implicit operator bool(Result result) => result.IsSuccess;
}

/// <summary>
/// Represents the result of an operation with a value, indicating success, failure, or absence of a value.
/// </summary>
/// <typeparam name="T">The type of the value associated with the result.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// The value associated with a successful result; null if the result is a failure or represents "None."
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Indicates whether the result represents "None" (no value and no error).
    /// </summary>
    public bool IsNone => !IsSuccess && EqualityComparer<T>.Default.Equals(Value, default) && Errors.Count == 0;

    /// <summary>
    /// Private constructor to initialize a successful Result with a value.
    /// </summary>
    /// <param name="value">The value associated with the successful result.</param>
    private Result(T value) : base(true)
    {
        Value = value;
    }

    /// <summary>
    /// Private constructor to initialize a failed Result with an error message.
    /// </summary>
    /// <param name="errors">The collection of errors describing the failure.</param>
    private Result(IReadOnlyCollection<DomainError> errors) : base(false, errors)
    {
    }

    /// <summary>
    /// Private constructor to initialize a Result representing "None" (no value and no error).
    /// </summary>
    private Result() : base(false, null)
    {
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value associated with the successful result.</param>
    /// <returns>A Result instance representing success with a value.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static new Result<T> Failure(string error) => new([DomainError.Create(error)]);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of errors describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result<T> Failure(IReadOnlyCollection<DomainError> errors) => new(errors);

    /// <summary>
    /// Creates a failed result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static new Result<T> Failure(Exception exception) => new([DomainError.Create(exception.Message)]);

    /// <summary>
    /// Creates a result representing "None" (no value and no error).
    /// </summary>
    /// <returns>A Result instance representing "None."</returns>
    public static Result<T> None() => new();

    /// <summary>
    /// Transforms the value of a successful result into another type while preserving the success or failure state.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">The function to transform the value.</param>
    /// <returns>A new Result instance with the transformed value or the original failure.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        return IsSuccess && !EqualityComparer<T>.Default.Equals(Value, default)
        ? Result<TOut>.Success(map(Value!))
        : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Chains operations that return a Result, propagating failures.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the resulting Result.</typeparam>
    /// <param name="bind">The function to apply to the value of a successful result.</param>
    /// <returns>A new Result instance from the chained operation or the original failure.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        return IsSuccess && !EqualityComparer<T>.Default.Equals(Value, default)
            ? bind(Value!)
            : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Implicitly converts a value of type T to a successful Result instance.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Result<T>(T value) => Success(value);
}

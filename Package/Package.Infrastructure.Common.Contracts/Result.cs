namespace Package.Infrastructure.Common.Contracts;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    private static readonly string[] s_emptyErrors = [];
    private static readonly Result s_success = new(true);

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
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the concatenated error messages from the list of errors.
    /// </summary>
    public string ErrorMessage => Errors.Count > 0 ? string.Join(", ", Errors) : string.Empty;

    /// <summary>
    /// Protected constructor to initialize a Result instance.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="errors">The collection of errors if the operation failed.</param>
    protected Result(bool isSuccess, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? s_emptyErrors;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A cached Result instance representing success.</returns>
    public static Result Success() => s_success;

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(string error) => new(false, [error]);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of errors describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);

    /// <summary>
    /// Creates a failed result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static Result Failure(Exception exception) => new(false, [exception.Message]);

    /// <summary>
    /// Transforms a successful result into a typed result with the provided value while preserving the success or failure state.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the resulting Result.</typeparam>
    /// <param name="value">The value to use for a successful result.</param>
    /// <returns>A new Result&lt;TOut&gt; instance with the provided value or the original failure.</returns>
    public Result<TOut> Map<TOut>(TOut value)
    {
        return IsSuccess
            ? Result<TOut>.Success(value)
            : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Transforms a successful result into a typed result using a factory function while preserving the success or failure state.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the resulting Result.</typeparam>
    /// <param name="factory">The function to create the value for a successful result.</param>
    /// <returns>A new Result&lt;TOut&gt; instance with the created value or the original failure.</returns>
    public Result<TOut> Map<TOut>(Func<TOut> factory)
    {
        return IsSuccess
            ? Result<TOut>.Success(factory())
            : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Executes one of two provided functions based on the result's state (success or failure).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is successful.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The value returned by the executed function.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<string>, TOut> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>
    /// Executes one of two provided asynchronous functions based on the result's state (success or failure).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
    /// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation, containing the value returned by the executed function.</returns>
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<IReadOnlyList<string>, Task<TOut>> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result, allowing for chaining.</returns>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute with the errors.</param>
    /// <returns>The original result, allowing for chaining.</returns>
    public Result OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }
        return this;
    }

    /// <summary>
    /// Combines multiple Result instances into one. If any result is a failure, the combined result is a failure with aggregated errors.
    /// </summary>
    /// <param name="results">The array of Result instances to combine.</param>
    /// <returns>A combined Result instance.</returns>
    public static Result Combine(params Result[] results)
    {
        List<string>? allErrors = null;
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                allErrors ??= [];
                allErrors.AddRange(result.Errors);
            }
        }

        return allErrors is null ? s_success : Failure(allErrors);
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
    private Result(IReadOnlyList<string> errors) : base(false, errors)
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
    public static new Result<T> Failure(string error) => new([error]);

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of errors describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static new Result<T> Failure(IReadOnlyList<string> errors) => new(errors);

    /// <summary>
    /// Creates a failed result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A Result instance representing failure.</returns>
    public static new Result<T> Failure(Exception exception) => new([exception.Message]);

    /// <summary>
    /// Creates a result representing "None" (no value and no error).
    /// </summary>
    /// <returns>A cached Result instance representing "None."</returns>
    public static Result<T> None() => NoneResult.Instance;

    /// <summary>
    /// Executes one of two provided functions based on the result's state (success or failure).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is successful.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <returns>The value returned by the executed function.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<string>, TOut> onFailure)
    {
        return IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>
    /// Executes one of three provided functions based on the result's state (success, failure, or none).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is successful.</param>
    /// <param name="onFailure">The function to execute if the result is a failure.</param>
    /// <param name="onNone">The function to execute if the result is none.</param>
    /// <returns>The value returned by the executed function.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<string>, TOut> onFailure, Func<TOut> onNone)
    {
        if (IsNone)
        {
            return onNone();
        }
        return IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>
    /// Executes one of two provided asynchronous functions based on the result's state (success or failure).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
    /// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
    /// <returns>A task that represents the asynchronous operation, containing the value returned by the executed function.</returns>
    public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<string>, Task<TOut>> onFailure)
    {
        return IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>
    /// Executes one of three provided asynchronous functions based on the result's state (success, failure, or none).
    /// </summary>
    /// <typeparam name="TOut">The return type of the functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function to execute if the result is successful.</param>
    /// <param name="onFailure">The asynchronous function to execute if the result is a failure.</param>
    /// <param name="onNone">The asynchronous function to execute if the result is none.</param>
    /// <returns>A task that represents the asynchronous operation, containing the value returned by the executed function.</returns>
    public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<string>, Task<TOut>> onFailure, Func<Task<TOut>> onNone)
    {
        if (IsNone)
        {
            return onNone();
        }
        return IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute with the value.</param>
    /// <returns>The original result, allowing for chaining.</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value is not null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Transforms the value of a successful result into another type while preserving the success or failure state.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">The function to transform the value.</param>
    /// <returns>A new Result instance with the transformed value or the original failure.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        if (IsSuccess && !EqualityComparer<T>.Default.Equals(Value, default))
        {
            return Result<TOut>.Success(map(Value!));
        }

        if (IsNone)
        {
            return Result<TOut>.None();
        }

        return Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Chains operations that return a Result, propagating failures.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the resulting Result.</typeparam>
    /// <param name="bind">The function to apply to the value of a successful result.</param>
    /// <returns>A new Result instance from the chained operation or the original failure.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        if (IsSuccess && !EqualityComparer<T>.Default.Equals(Value, default))
        {
            return bind(Value!);
        }

        if (IsNone)
        {
            return Result<TOut>.None();
        }

        return Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Chains operations, but treats None results as successful continuation, preserving the original value.
    /// Use when None represents a valid "no-op" rather than failure.
    /// </summary>
    /// <typeparam name="TIgnore">The type of the value in the bind operation result (ignored if None).</typeparam>
    /// <param name="bind">The function to apply to the value of a successful result.</param>
    /// <returns>The original result if the bind operation succeeds or returns None; otherwise, the failure from the bind operation.</returns>
    public Result<T> BindOrContinue<TIgnore>(Func<T, Result<TIgnore>> bind)
    {
        if (!IsSuccess || EqualityComparer<T>.Default.Equals(Value, default))
        {
            if (IsNone)
            {
                return Result<T>.None();
            }
            return Result<T>.Failure(Errors);
        }

        var result = bind(Value!);

        // If the operation succeeds or returns None, preserve original value
        return result.IsFailure
            ? Result<T>.Failure(result.Errors)
            : Result<T>.Success(Value!);
    }

    /// <summary>
    /// Applies a side effect operation that may fail, but preserves the original value on success or None.
    /// Use for operations like logging, auditing, or updating related data where the side effect shouldn't affect the main result.
    /// </summary>
    /// <param name="sideEffect">The side effect function to apply to the value of a successful result.</param>
    /// <returns>The original result if the side effect succeeds or returns None; otherwise, the failure from the side effect.</returns>
    public Result<T> Tap(Func<T, Result> sideEffect)
    {
        if (!IsSuccess || EqualityComparer<T>.Default.Equals(Value, default))
        {
            if (IsNone)
            {
                return Result<T>.None();
            }
            return Result<T>.Failure(Errors);
        }

        var result = sideEffect(Value!);

        return result.IsFailure
            ? Result<T>.Failure(result.Errors)
            : Result<T>.Success(Value!);
    }

    /// <summary>
    /// Applies a side effect operation that returns a typed result, but preserves the original value on success or None.
    /// Use for operations like validation or updating related data where the side effect result type differs from the main value.
    /// </summary>
    /// <typeparam name="TIgnore">The type of the value in the side effect result (ignored if successful or None).</typeparam>
    /// <param name="sideEffect">The side effect function to apply to the value of a successful result.</param>
    /// <returns>The original result if the side effect succeeds or returns None; otherwise, the failure from the side effect.</returns>
    public Result<T> Tap<TIgnore>(Func<T, Result<TIgnore>> sideEffect)
    {
        if (!IsSuccess || EqualityComparer<T>.Default.Equals(Value, default))
        {
            if (IsNone)
            {
                return Result<T>.None();
            }
            return Result<T>.Failure(Errors);
        }

        var result = sideEffect(Value!);

        return result.IsFailure
            ? Result<T>.Failure(result.Errors)
            : Result<T>.Success(Value!);
    }

    /// <summary>
    /// Implicitly converts a value of type T to a successful Result instance.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Result<T>(T value) => Success(value);

    private static class NoneResult
    {
        internal static readonly Result<T> Instance = new();
    }
}

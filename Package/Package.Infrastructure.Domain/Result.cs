namespace Package.Infrastructure.Domain;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Contains the error message if the operation failed; otherwise, null.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Protected constructor to initialize a Result instance.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed; otherwise, null.</param>
    protected Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
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
    public static Result Failure(string error) => new(false, error);

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
    public bool IsNone => !IsSuccess && Error == null;

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
    /// <param name="error">The error message describing the failure.</param>
    private Result(string error) : base(false, error)
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
    public static new Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Creates a result representing "None" (no value and no error).
    /// </summary>
    /// <returns>A Result instance representing "None."</returns>
    public static Result<T> None() => new Result<T>();
}

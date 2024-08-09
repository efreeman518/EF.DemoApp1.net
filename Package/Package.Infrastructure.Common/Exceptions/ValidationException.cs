namespace Package.Infrastructure.Common.Exceptions;

public class ValidationException : Exception
{
    public readonly ValidationResult? ValidationResult = null;

    public ValidationException() : base()
    {
    }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(ValidationResult validationResult)
        : base(string.Join<string>("; ", [.. validationResult.Messages]))
    {
        ValidationResult = validationResult;
    }

    public ValidationException(List<string> errors)
        : base(string.Join<string>("; ", [.. errors]))
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(string.Join<string>("; ", errors.Select(e => $"{(e.Key.Length > 0 ? e.Key + ": " : "")}{string.Join<string>(", ", e.Value)}")))
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

}

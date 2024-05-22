namespace Package.Infrastructure.Common.Exceptions;

public class ValidationException : Exception
{

    public ValidationException() : base()
    {
    }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(ValidationResult validationResult)
        : base(string.Join<string>("; ", [.. validationResult.Messages]))
    {
    }

    public ValidationException(List<string> errors)
        : base(string.Join<string>("; ", [.. errors]))
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

}

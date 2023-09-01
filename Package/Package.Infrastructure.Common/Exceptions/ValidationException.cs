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
        : base(string.Join("; ", validationResult.Messages.ToArray()))
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }
}

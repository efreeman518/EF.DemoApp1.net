using System.Runtime.Serialization;

namespace Package.Infrastructure.Common.Exceptions;

[Serializable]
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

}

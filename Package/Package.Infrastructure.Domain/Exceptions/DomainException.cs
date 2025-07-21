namespace Package.Infrastructure.Domain.Exceptions;
public class DomainException : Exception
{
    public DomainException() : base()
    {
    }

    public DomainException(string message) : base(message)
    {
    }
}

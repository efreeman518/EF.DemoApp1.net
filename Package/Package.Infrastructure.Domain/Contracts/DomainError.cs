namespace Package.Infrastructure.Domain.Contracts;
public record DomainError(string Error, string? Code = null)
{
    public static DomainError Create(string error, string? code = null) => new(error, code);
}

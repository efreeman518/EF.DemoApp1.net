namespace Package.Infrastructure.Domain;
public class DomainError
{
    public static DomainError Create(string error)
    {
        return new DomainError { Error = error };
    }
    public string Error { get; init; } = null!;
}

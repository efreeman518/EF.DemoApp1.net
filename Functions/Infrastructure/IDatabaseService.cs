namespace Functions.Infrastructure;

public interface IDatabaseService
{
    Task MethodAsync(string? filename, CancellationToken cancellationToken = default);
}

namespace Package.Infrastructure.Test.Integration.Service;

public interface ISomeScopedService
{
    public Task SomeAsyncWork(CancellationToken cancellationToken);
}

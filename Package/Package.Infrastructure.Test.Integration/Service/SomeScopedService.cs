namespace Package.Infrastructure.Test.Integration.Service;

public class SomeScopedService(IServiceProvider serviceProvider) : ISomeScopedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task SomeAsyncWork(CancellationToken cancellationToken)
    {
        _ = _serviceProvider.GetHashCode();
        await Task.Delay(1000, cancellationToken);
    }
}

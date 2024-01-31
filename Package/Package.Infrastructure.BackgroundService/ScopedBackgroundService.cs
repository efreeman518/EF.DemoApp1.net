using Microsoft.Extensions.DependencyInjection;

namespace Package.Infrastructure.BackgroundServices;

public abstract class ScopedBackgroundService(IServiceScopeFactory serviceScopeFactory) : Microsoft.Extensions.Hosting.BackgroundService()
{
    //Inheriting class must implement
    protected abstract Task ExecuteInScope(IServiceProvider serviceProvider, string TraceId, CancellationToken stoppingToken = default);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await ExecuteInScope(scope.ServiceProvider, Guid.NewGuid().ToString(), stoppingToken);
    }
}

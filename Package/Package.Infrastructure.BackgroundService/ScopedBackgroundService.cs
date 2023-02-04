using Microsoft.Extensions.DependencyInjection;

namespace Package.Infrastructure.BackgroundService;
public abstract class ScopedBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    //Inheriting class must implement
    protected abstract Task ExecuteInScope(IServiceProvider serviceProvider, string TraceId, CancellationToken stoppingToken = default);

    protected ScopedBackgroundService(IServiceScopeFactory serviceScopeFactory) : base()
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await ExecuteInScope(scope.ServiceProvider, Guid.NewGuid().ToString(), stoppingToken);
    }
}

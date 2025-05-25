using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Package.Infrastructure.BackgroundServices.InternalMessageBroker;

namespace SampleApp.Bootstrapper;

public static class IHostExtensions
{
    public static async Task RunStartupTasks(this IHost host)
    {
        //register message handlers
        var msgBus = host.Services.GetRequiredService<IInternalMessageBus>();
        msgBus.AutoRegisterHandlers(); //auto-register all IMessageHandler<T> implementations in the current AppDomain

        using var scope = host.Services.CreateScope();
        var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>();

        //Run each startup task
        foreach (var startupTask in startupTasks)
        {
            await startupTask.ExecuteAsync();
        }
    }
}
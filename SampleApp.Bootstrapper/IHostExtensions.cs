using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleApp.Bootstrapper;

public static class IHostExtensions
{
    public static async Task RunStartupTasks(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>();

        //Run each startup task
        foreach (var startupTask in startupTasks)
        {
            await startupTask.ExecuteAsync();
        }
    }
}
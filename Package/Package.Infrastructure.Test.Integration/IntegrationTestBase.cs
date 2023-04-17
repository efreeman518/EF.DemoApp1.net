using Infrastructure.BackgroundServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.CosmosDb;

namespace Package.Infrastructure.Test.Integration;

[TestClass]
public abstract class IntegrationTestBase
{
    protected readonly IConfiguration Config;
    protected readonly IServiceProvider Services;
    protected readonly ILoggerFactory LoggerFactory;

    //[AssemblyInitialize]
    //public static void Initialize(TestContext ctx) //ctx required for [AssemblyInitialize] run 
    protected IntegrationTestBase()
    {
        //Configuration
        Config = Utility.BuildConfiguration<IntegrationTestBase>();

        //DI
        ServiceCollection services = new();

        //queued background service - fire and forget 
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        //CosmosDb
        services.AddSingleton(provider =>
        {
            return new CosmosDbRepositorySettings
            {
                CosmosClient = new CosmosClient(Config.GetConnectionString("CosmosDB")),
                DbId = Config.GetValue<string>("CosmosDbId")
            };
        });
        services.AddScoped<CosmosDbRepository>();

        services.AddLogging(configure => configure.AddConsole().AddDebug());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
    }

}

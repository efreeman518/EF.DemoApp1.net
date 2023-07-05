using Azure;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.CosmosDb;
using Package.Infrastructure.Messaging;
using Package.Infrastructure.OpenAI.ChatApi;
using Package.Infrastructure.Storage;

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

        //Azure Service Clients - Blob, EventGridPublisher, KeyVault, etc; enables injecting IAzureClientFactory<>
        //https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection
        //https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.azure.azureclientfactorybuilder?view=azure-dotnet
        //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Microsoft.Extensions.Azure/1.0.0/index.html
        services.AddAzureClients(builder =>
        {
            // Set up any default settings
            builder.ConfigureDefaults(Config.GetSection("AzureClientDefaults"));
            // Use DefaultAzureCredential by default
            builder.UseCredential(new DefaultAzureCredential());

            //Ideally use ServiceUri (w/DefaultAzureCredential)
            builder.AddBlobServiceClient(Config.GetSection("ConnectionStrings:BlobStorage")).WithName("AzureBlobStorageAccount1");

            //Ideally use TopicEndpoint Uri (w/DefaultAzureCredential)
            builder.AddEventGridPublisherClient(new Uri(Config.GetValue<string>("EventGridPublisher1:TopicEndpoint")!),
                new AzureKeyCredential(Config.GetValue<string>("EventGridPublisher1:Key")!))
                .WithName("EventGridPublisher1");
        });

        //BlobStorageManager (injected with IAzureClientFactory<BlobServiceClient>)
        services.AddSingleton<IAzureBlobStorageManager, AzureBlobStorageManager>();
        services.Configure<AzureBlobStorageManagerSettings>(Config.GetSection(AzureBlobStorageManagerSettings.ConfigSectionName));

        //EventGridPublisherManager (injected with IAzureClientFactory<BlobServiceClient>)
        services.AddSingleton<IEventGridPublisherManager, EventGridPublisherManager>();
        services.Configure<EventGridPublisherManagerSettings>(Config.GetSection(EventGridPublisherManagerSettings.ConfigSectionName));

        //CosmosDb
        services.AddTransient<ICosmosDbRepository, CosmosDbRepository>();
        services.AddSingleton(provider =>
        {
            return new CosmosDbRepositorySettings
            {
                CosmosClient = new CosmosClient(Config.GetConnectionString("CosmosDB")),
                DbId = Config.GetValue<string>("CosmosDbId")
            };
        });

        //OpenAI chat service
        services.AddTransient<IChatService, ChatService>();
        services.Configure<ChatServiceSettings>(Config.GetSection(ChatServiceSettings.ConfigSectionName));

        services.AddLogging(configure => configure.AddConsole().AddDebug());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
    }

}

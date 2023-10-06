using Application.Contracts.Interfaces;
using Azure;
using Azure.Identity;
using Infrastructure.RapidApi.WeatherApi;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.OpenAI.ChatApi;
using Package.Infrastructure.Test.Integration.Blob;
using Package.Infrastructure.Test.Integration.Cosmos;
using Package.Infrastructure.Test.Integration.KeyVault;
using Package.Infrastructure.Test.Integration.Messaging;
using Package.Infrastructure.Test.Integration.Service;
using Package.Infrastructure.Test.Integration.Table;

namespace Package.Infrastructure.Test.Integration;

[TestClass]
public abstract class IntegrationTestBase
{
    protected readonly IConfiguration Config;
    protected readonly IServiceProvider Services;
    protected readonly ILogger<IntegrationTestBase> Logger;

    //[AssemblyInitialize]
    //public static void Initialize(TestContext ctx) //ctx required for [AssemblyInitialize] run 
    //{ }

    protected IntegrationTestBase()
    {
        //Configuration
        Config = Utility.BuildConfiguration<IntegrationTestBase>();

        //DI
        ServiceCollection services = new();

        //add logging for integration tests
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug());

        //queued background service - fire and forget 
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        IConfigurationSection configSection;

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

            //Blob 
            configSection = Config.GetSection("ConnectionStrings:AzureBlobStorageAccount1");
            if (configSection.Exists())
            {
                //Ideally use ServiceUri (w/DefaultAzureCredential)
                builder.AddBlobServiceClient(configSection).WithName("AzureBlobStorageAccount1");
            }

            //Table 
            configSection = Config.GetSection("ConnectionStrings:AzureTable1");
            if (configSection.Exists())
            {
                //Ideally use ServiceUri (w/DefaultAzureCredential)
                builder.AddTableServiceClient(configSection).WithName("AzureTable1");
            }

            //EventGrid Publisher
            configSection = Config.GetSection("EventGridPublisherTopic1");
            if (configSection.Exists())
            {
                //Ideally use TopicEndpoint Uri (w/DefaultAzureCredential)
                builder.AddEventGridPublisherClient(new Uri(configSection.GetValue<string>("TopicEndpoint")!),
                    new AzureKeyCredential(configSection.GetValue<string>("Key")!))
                .WithName("EventGridPublisherTopic1");
            }

            //KeyVault
            configSection = Config.GetSection("KeyVaultManager1");
            if (configSection.Exists())
            {
                //(w/DefaultAzureCredential)
                builder.AddSecretClient(new Uri(configSection.GetValue<string>("VaultUrl")!)).WithName("KeyVaultManager1");
                builder.AddKeyClient(new Uri(configSection.GetValue<string>("VaultUrl")!)).WithName("KeyVaultManager1");
                builder.AddCertificateClient(new Uri(configSection.GetValue<string>("VaultUrl")!)).WithName("KeyVaultManager1");
            }
        });

        //BlobStorage
        configSection = Config.GetSection(BlobRepositorySettings1.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddSingleton<IBlobRepository1, BlobRepository1>();
            services.Configure<BlobRepositorySettings1>(configSection);
        }

        //TableStorage
        configSection = Config.GetSection(TableRepositorySettings1.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddSingleton<ITableRepository1, TableRepository1>();
            services.Configure<TableRepositorySettings1>(configSection);
        }

        //EventGridPublisher
        configSection = Config.GetSection(EventGridPublisherSettings1.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddSingleton<IEventGridPublisher1, EventGridPublisher1>();
            services.Configure<EventGridPublisherSettings1>(configSection);
        }

        //KeyVaultManager
        configSection = Config.GetSection(KeyVaultManagerSettings1.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddSingleton<IKeyVaultManager1, KeyVaultManager1>();
            services.Configure<KeyVaultManagerSettings1>(configSection);
        }

        //CosmosDb - CosmosClient is thread-safe. Its recommended to maintain a single instance of CosmosClient per lifetime of the application which enables efficient connection management and performance.
        var connectionString = Config.GetConnectionString("CosmosClient1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            configSection = Config.GetSection(CosmosDbRepositorySettings1.ConfigSectionName);
            if (configSection.Exists())
            {
                services.AddTransient<ICosmosDbRepository1, CosmosDbRepository1>();
                services.Configure<CosmosDbRepositorySettings1>(s =>
                {
                    s.CosmosClient = new CosmosClientBuilder(connectionString) //(AccountEndpoint, DefualtAzureCredential())
                                                                               //.With...options
                        .Build();
                    s.CosmosDbId = configSection.GetValue<string>("CosmosDbId")!;
                });
            }
        }

        //external weather service
        configSection = Config.GetSection(WeatherServiceSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.Configure<WeatherServiceSettings>(configSection);
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddHttpClient<IWeatherService, WeatherService>(client =>
            {
                client.BaseAddress = new Uri(Config.GetValue<string>("WeatherServiceSettings:BaseUrl")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", Config.GetValue<string>("WeatherServiceSettings:Key")!);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", Config.GetValue<string>("WeatherServiceSettings:Host")!);
            })
            .AddPolicyHandler(PollyRetry.GetHttpRetryPolicy())
            .AddPolicyHandler(PollyRetry.GetHttpCircuitBreakerPolicy());
        }

        //OpenAI chat service
        configSection = Config.GetSection(ChatServiceSettings.ConfigSectionName);
        if (configSection.Exists())
        {
            services.AddTransient<IChatService, ChatService>();
            services.Configure<ChatServiceSettings>(configSection);
        }

        //Sample scoped service for testing BackgroundTaskQueue.QueueScopedBackgroundWorkItem
        services.AddScoped<ISomeScopedService, SomeScopedService>();

        services.AddLogging(configure => configure.AddConsole().AddDebug());

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider();

        //LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();
        Logger.Log(LogLevel.Information, "Test Initialized.");
    }
}

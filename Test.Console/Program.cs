//logging for initialization
using Application.Contracts.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.SampleApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Auth.Tokens;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Grpc;
using Test.Console;
using SampleAppGrpc = SampleApp.Grpc.Proto;
using SampleAppModel = Application.Contracts.Model;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddDebug();
    builder.AddConsole();
    builder.AddApplicationInsights();
});
var logger = loggerFactory.CreateLogger<Program>()!;

var config = Utility.Config;

//DI
IServiceCollection services = new ServiceCollection();
//app insights telemetry logging for non-http service
services
    .AddLogging(configure => configure.AddConsole().AddDebug().AddApplicationInsights())
    .AddApplicationInsightsTelemetryWorkerService(config);

//Rest Client
services.Configure<SampleApiRestClientSettings>(config.GetSection(SampleApiRestClientSettings.ConfigSectionName));
services.AddTransient(provider =>
{
    //DefaultAzureCredential checks env vars first, then checks other - managed identity, etc
    //so if we need to use client/secret (client AAD App Reg), set the env vars
    if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
    {
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
    }
    var scopes = config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();
    return new SampleRestApiAuthMessageHandler(scopes!);
});
services.AddHttpClient<ISampleApiRestClient, SampleApiRestClient>(options =>
{
    options.BaseAddress = new Uri(config.GetValue<string>("SampleApiRestClientSettings:BaseUrl")!); //HttpClient will get injected
    //options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
}).AddHttpMessageHandler<SampleRestApiAuthMessageHandler>();


//GRPC Client
services.AddTransient<ClientErrorInterceptor>(); //should be scoped in prod
services.AddGrpcClient2<SampleAppGrpc.TodoService.TodoServiceClient>(
    baseAddress: new Uri(config.GetValue<string>("SampleApiGrpcClientSettings:ServiceUrl")!)
    //authHeaderValue: new AuthenticationHeaderValue("bearer", token)
    )
.AddInterceptor<ClientErrorInterceptor>();

//LazyCache needed for AzureAdTokenProviderDefaultAzureCred
services.AddLazyCache();
services.AddSingleton<IAzureAdTokenRetriever, AzureAdTokenProviderDefaultAzureCred>();

//build IServiceProvider for subsequent use finding / injecting services
IServiceProvider serviceProvider = services.BuildServiceProvider(validateScopes: true);

//REST
var restClient = serviceProvider.GetRequiredService<ISampleApiRestClient>();
//GRPC - Azure App Service requires config setting Http Version = 2, Http 2.0 Proxy = On, and an app setting HTTP20_ONLY_PORT to any? port number
var grpcClient = serviceProvider.GetRequiredService<SampleAppGrpc.TodoService.TodoServiceClient>();

//Console UI
ShowCommands();

string? command = null;
string? input1 = null;
string? input2 = null;
Guid id;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("Enter command:");
    command = Console.ReadLine();
    if (command?.ToLower() == "exit") break;

    switch (command)
    {
        case "r-page":
            //REST
            await AttemptRestAsync(() => restClient.GetPageAsync());
            break;
        case "g-page":
            //GRPC
            await AttemptGrpcAsync(async () => await grpcClient.PageAsync(new SampleAppGrpc.ServiceRequestPage { Pagesize = 10, Pageindex = 1 }));
            break;
        case "r-get":
        case "g-get":
            Console.WriteLine("Id (Enter for null):");
            input1 = Console.ReadLine();
            if (!Guid.TryParse(input1, out id))
            {
                Console.WriteLine("Id must be a valid Guid.");
                break;
            }

            if (command.Contains("r-"))
            {
                //REST
                await AttemptRestAsync(() => restClient.GetItemAsync(id));
            }
            else
            {
                //GRPC
                await AttemptGrpcAsync(async () => await grpcClient.GetAsync(new SampleAppGrpc.ServiceRequestId { Id = id.ToString() }));
            }
            break;

        case "r-save":
        case "g-save":
            Console.WriteLine("Id (Enter for null):");
            input1 = Console.ReadLine();
            if (!Guid.TryParse(input1, out id) && (input1 != ""))
            {
                Console.WriteLine("Id must be null or a valid Guid.");
                break;
            }
            Console.WriteLine("Name:");
            input2 = Console.ReadLine();

            if (command.Contains("r-"))
            {
                //REST
                await AttemptRestAsync(() => restClient.SaveItemAsync(new SampleAppModel.TodoItemDto { Id = id, Name = input2 ?? Guid.NewGuid().ToString() }));
            }
            else
            {
                //GRPC
                await AttemptGrpcAsync(async () => await grpcClient.SaveAsync(new SampleAppGrpc.ServiceRequestTodoItem
                {
                    Data = new SampleAppGrpc.TodoItemDto
                    {
                        //setting NullableString - set only one property (Data or Isnull); setting both - the first is removed from the structure
                        Id = id == Guid.Empty ? new SampleAppGrpc.NullableString { Isnull = true } : new SampleAppGrpc.NullableString { Data = id.ToString() },
                        Name = new SampleAppGrpc.NullableString { Data = input2 }
                    }
                }));
            }
            break;

        case "r-delete":
        case "g-delete":
            Console.WriteLine("Id:");
            input1 = Console.ReadLine();
            if (!Guid.TryParse(input1, out id))
            {
                Console.WriteLine("Id must be a valid Guid.");
                break;
            }

            if (command.Contains("r-"))
            {
                //REST
                await AttemptRestAsync(() => (Task<object>)restClient.DeleteItemAsync(id));
            }
            else
            {
                //GRPC
                await AttemptGrpcAsync<Empty>(async () => await grpcClient.DeleteAsync(new SampleAppGrpc.ServiceRequestId { Id = id.ToString() }));
            }
            break;
        case "r-getuser":
            await AttemptRestAsync(() => restClient.GetUserAsync());
            break;
        case "r-getuserclaims":
            await AttemptRestAsync(() => restClient.GetUserClaimsAsync());
            break;
        default:
            Console.WriteLine("Enter a valid command.");
            ShowCommands();
            break;
    }
}

async Task AttemptRestAsync<T>(Func<Task<T>> method)
{
    logger.InfoLog("REST Client initiate request");
    Console.WriteLine("----------REST Client initiate request -----------");

    try
    {
        var response = await method();
        Console.WriteLine("----------REST Client handles response -----------");
        Console.WriteLine($"{response?.SerializeToJson() ?? ""}");
        //if (response?.Errors?.Count > 0)
        //{
        //    Console.WriteLine($"Errors: {response.Errors.Aggregate(new StringBuilder(), (sb, a) => sb.AppendLine(String.Join(", ", a.Message)), sb => sb.ToString())}");
        //}
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}

async Task AttemptGrpcAsync<T>(Func<Task<T>> method)
{
    logger.InfoLog("REST Client initiate request");
    Console.WriteLine("----------GRPC Client initiate request -----------");
    T? response;
    try
    {
        response = await method();
        Console.WriteLine("----------GRPC Client handles response -----------");
        Console.WriteLine($"{response?.SerializeToJson() ?? ""}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}

static void ShowCommands()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("r-page   - HTTP GET todoitems");
    Console.WriteLine("r-get    - HTTP GET todoitems/{id}");
    Console.WriteLine("r-save   - HTTP POST/PUT todoitems/{id?}");
    Console.WriteLine("r-delete - HTTP DELETE todoitems/{id}");
    Console.WriteLine("g-page   - GRPC PageAsync");
    Console.WriteLine("g-get    - GRPC GetAsync");
    Console.WriteLine("g-save   - GRPC SaveAsync");
    Console.WriteLine("g-delete - GRPC DeleteAsync");
    Console.WriteLine("exit     - Exit");
}

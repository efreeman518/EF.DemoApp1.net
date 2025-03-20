using Azure.AI.OpenAI;
using Azure.Identity;
using Console.AI1.Demo.MSExtAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


//setup for convenience - IHostBuilder gives us a service collection
var hostBuilder = Host.CreateApplicationBuilder(args);
var configBuilder = new ConfigurationBuilder();
configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
configBuilder.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
configBuilder.AddEnvironmentVariables();
configBuilder.AddUserSecrets<Program>();
var config = configBuilder.Build();
hostBuilder.Configuration.AddConfiguration(config);
var services = hostBuilder.Services;
var creds = new DefaultAzureCredential();

var aoaiConfig = config.GetSection("AzureOpenAI");
var endpoint = aoaiConfig.GetValue<string>("Endpoint")!;
AzureOpenAIClient aoaiClient = new(new Uri(endpoint), creds, new AzureOpenAIClientOptions());

//multi agent chat
//await new MultiAgentChat(config, services, aoaiClient).RunAsync();
await new MSExtAIChat(config, services, aoaiClient).RunAsync();








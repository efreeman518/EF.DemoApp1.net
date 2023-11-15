# Introduction 
C# Sample App - A modern domain-centric service api template
- main branch - .net7 latest release
- net8 branch - .net8 latest preview release

# Prerequisites
1. [Visual Studio 2022 Latest (>=17.8 preview needed for .net8)](https://visualstudio.microsoft.com/vs/)
2. [.net7 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (main branch)
3. [.net8 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (net8 branch, will merge to main when released)

# Getting Started
Clone, set the startup project to SampleApp/SampleApp.Api, and run - swagger page opens, using the same port and root path shows a basic js UI

# Run tests
* Test projects (MSTest) include:
	* Test.Unit - unit tests using in-memory DBContext, [CleanMoq](https://github.com/hassanhabib/CleanMoq), mutation testing using [Stryker.Net](https://stryker-mutator.io/) see stryker-config.json
	* Test.Integration - integration tests
	* Test.UI - browser UI tests using [Selenium](https://selenium.dev/)
	* Test.SpecFlow.Specs - BDD tests using [SpecFlow](https://specflow.org/) and [Selenium](https://selenium.dev/)
	* Test.Endpoints - api endpoint testing using MS [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0)
	* Test.Load - load testing using [NBomber](https://nbomber.com/)
	* Test.Benchmarks - benchmark testing using [BenchmarkDotNet](https://benchmarkdotnet.org/)
	* Test.Console - console app tester for manually hitting enpoints
* Some tests are ignored by default because they require further setup/integration
	* Test.UI, Test.SpecFlow.Specs (UI feature) tests use Selenium which requires the appropriate versions of Chrome and Edge. 
	* Test.Load, Test.UI, Test.SpecFlow.Specs (UI feature) require the app to be running in order to run the Load & Selenium UI tests. Load and run the solution in one VS, load into another VS to run the UI dependent tests.
	* AzureBlobStorageTests needs either Azurite (https://www.npmjs.com/package/azurite) or an Azure storage account
	* CosmosDbTests needs either the CosmosDb emulator (https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21) or an Azure CosmosDb Account
	* AzureTableRepositoryTests needs Azurite or Storage or Cosmos Table Api
	* KeyVaultManagerTests needs an Azure KeyVault
	* RapidApi tests require credentials (key & host headers) - https://rapidapi.com/ (free account)
	* OpenAI Api tests require credentials (key) - https://platform.openai.com/docs/introduction/overview (paid account)
   
# Azure Functions App
* Running the Azure Functions project requires various integrations set up
* Blob/Queue Functions - run Azurite or a real storage account
* EventGrid Functions - tunnel back to local using VS Dev Tunnels (public) or ngrok url (auto-validate) EventGrid-Topic-Subscription url/runtime/webhooks/EventGrid?functionName=EventGridTriggerCustom

# Notes
1. Started from Microsoft's Todo sample api (<a href="https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio" target="_blank">https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio</a>)
2. This sample uses Entity Framework Core in-memory DbContext by default, so restarting the app clears the DB
3. This solution provides a possible starting template for building service apis and is not production-ready (in-memory database, no authentication, etc)
4. Package.Infrastructure projects are meant to reside in a nuget package feed, but for simplicity and portability of this sample, the source projects are included and referenced
5. Package.Infrastructure.Storage & Functions.FunctionBlobTrigger - install and run latest azurite storage emulator (https://www.npmjs.com/package/azurite)
   * npm install -g azurite
   * (if needed for Powershell) Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   * azurite -s -l c:\azurite -d c:\temp\azurite\debug.log
6. Package.Infrastructure.CosmosDb - install and run latest CosmosDB emulator
   * https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21
7. Azure Storage Explorer is usefull for blobs/queues/tables
   * https://azure.microsoft.com/en-us/products/storage/storage-explorer/
8. Tunneling for EventGrid Functions - use VS Dev Tunnels or ngrok/tunnel to https://localhost:port IIS express
   * ngrok http https://localhost:44339 --host-header="localhost:44339"
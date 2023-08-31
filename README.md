# Introduction 
.net7 C# Sample App - A modern domain-centric service api template

# Prerequisites
1. [Visual Studio 2022 Latest (>=17.4.0)](https://visualstudio.microsoft.com/vs/)
2. [.net8 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

# Getting Started
1. Clone, set the startup project to SampleApp/SampleApp.Api, and run - swagger page opens, using the same port and root folder shows a basic js UI
2. Run tests - some are ignored by default because they require further setup/integration
   * Test.UI, Test.SpecFlow.Specs (UI feature) tests require the appropriate versions of Chrome and Edge. 
   * Test.Load, Test.UI, Test.SpecFlow.Specs (UI feature) require the app to be running in order to run the Load & Selenium UI tests. Load and run the solution in one VS, load into another VS to run the UI dependent tests.
   * RapidApi tests require credentials (key & host headers) - https://rapidapi.com/ (free account)
   * OpenAI Api tests require credentials (key) - https://platform.openai.com/docs/introduction/overview (paid account)
   * AzureBlobStorageTests needs either Azurite (https://www.npmjs.com/package/azurite) or an Azure storage account
   * CosmosDbTests needs either the CosmosDb emulator (https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21) or an Azure CosmosDb Account
3. Functions - running these require various integrations set up
   * Blob/Queue Functions - run Azurite or a real storage account
   * EventGrid Functions - tunnel back to local using VS Tunnels (public) or ngrok url (auto-validate) EventGrid-Topic-Subscription url/runtime/webhooks/EventGrid?functionName=EventGridTriggerCustom

# Notes
1. Started from Microsoft's Todo sample api (<a href="https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio" target="_blank">https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio</a>)
2. This sample uses Entity Framework Core in-memory DbContext, so restarting the app clears the DB
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
8. ngrok/tunnel to https://localhost:port IIS express
   * ngrok http https://localhost:44339 --host-header="localhost:44339"
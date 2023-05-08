# Introduction 
.net7 C# Sample App - A modern domain-centric service api template

# Prerequisites
1. [Visual Studio 2022 Latest (>=17.4.0)](https://visualstudio.microsoft.com/vs/)
2. [.net7 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

# Getting Started
1. Clone, set the startup project to SampleApp/SampleApp.Api, and run - swagger page opens, using the same port and root folder shows a basic js UI
2. Run tests - some are ignored by default because they require further setup/integration
   * Test.UI, Test.SpecFlow.Specs (UI feature) tests require the appropriate versions of Chrome and Edge. 
   * Test.Load, Test.UI, Test.SpecFlow.Specs (UI feature) require the app to be running in order to run the Load & Selenium UI tests. Load and run the solution in one VS, load into another VS to run the UI dependent tests.
   * Package.Infrastructure.Tests.Integration
     * AzureBlobStorageTests needs either Azurite or a real storage account
     * CosmosDbTests needs either the CosmosDb emulator or a real CosmosDb running
3. Functions - running these require various integrations set up
   * Blob/Queue functions - run Azurite or a real storage account
   * EventGrid functions - tunnel back using VS or ngrok

# Notes
1. Started from Microsoft's Todo sample api (<a href="https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio" target="_blank">https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio</a>)
2. This sample uses Entity Framework Core in-memory DbContext, so restarting the app clears the DB
3. This solution provides a possible starting template for building service apis and is not production-ready (in-memory database, no authentication, etc)
4. Package.Infrastructure projects are meant to reside in a nuget package feed, but for simplicity of this sample, the source projects are included and referenced
5. Infrastructure.RapidApi services and associated integration tests require credentials (key & host headers) from https://rapidapi.com/ (set up free account)
6. Package.Infrastructure.Storage & Functions.FunctionBlobTrigger - install and run latest azurite storage emulator (https://www.npmjs.com/package/azurite)
   * npm install -g azurite
   * (if needed for Powershell) Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   * azurite -s -l c:\azurite -d c:\azurite\debug.log
7. Package.Infrastructure.CosmosDb - install and run latest CosmosDB emulator
   * https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21
8. Azure Storage Explorer is usefull for blobs/queues/tables
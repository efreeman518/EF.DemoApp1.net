# Introduction 
C# Sample App - A modern domain-centric service api reference template

# Prerequisites
1. Visual Studio 2022 Latest
2. .NET 10 SDK

# Getting Started
Clone, set the startup project to SampleApp/SampleApp.Api, and run - openapi page (scalar) opens; using the same port and root path shows a basic js UI

# Run tests
* Test projects (MSTest) include:
	* Test.Unit - unit tests using in-memory DBContext, CleanMoq, mutation testing using Stryker.Net (see stryker-config.json)
	* Test.Integration - integration tests, optional SQL DB TestContainers
	* Test.Endpoints - api endpoint testing using MS WebApplicationFactory and optional SQL DB TestContainers
	* Test.PlaywrightUI - browser UI tests using SpecFlow and Playwright
	* Test.SeleniumUI - browser UI tests using Selenium
	* Test.SpecFlow - BDD tests using SpecFlow and Selenium
	* Test.Load - load testing using NBomber
	* Test.Benchmarks - benchmark testing using BenchmarkDotNet and optional SQL DB TestContainers
	* Test.Architecture - architecture tests using NetArchTest.Rules
	* Test.Console - console app tester for manually hitting the api http & gRPC endpoints

* Test.Integration, Test.Endpoints, Test.Benchmarks can be configured to spin up a DB container which requires a local container environment Docker Desktop

* Some tests are ignored by default because they require further setup/integration
    * Test.PlaywrightUI uses Playwright which requires browser binaries to be installed (pwsh bin/Debug/net10.0/playwright.ps1 install).
	* Test.SeleniumUI, Test.SpecFlow (UI feature) tests use Selenium which requires the appropriate versions of Chrome and Edge. 
	* Test.Load, Test.PlaywrightUI, Test.SeleniumUI, Test.SpecFlow (UI feature) require the app to be running in order to run the Load, Playwright, & Selenium UI tests. Open and run the solution in one VS, open into another VS instance to run the UI dependent tests.
	* AzureBlobStorageTests needs either Azurite or an Azure storage account
	* CosmosDbTests needs either the CosmosDb emulator or an Azure CosmosDb Account
	* AzureTableRepositoryTests needs Azurite or Storage or Cosmos Table Api
	* KeyVaultManagerTests needs an Azure KeyVault
	* RapidApi tests require credentials (key & host headers)
	* OpenAI Api tests require credentials (key)

* Chaos testing can be configured in the the api appsettings.json file for introducing chaos to backend (external) api calls

# Azure Functions App
* Ensure latest Azure Functions tooling is installed (VS-Tools-Options-Projects & Solutions-Azure Functions-Check For Updates)
* Running the Azure Functions project requires various integrations set up
* Blob/Queue Functions - run Azurite or a real storage account
* EventGrid Functions - tunnel back to local using VS Dev Tunnels (public) or ngrok url (auto-validate) EventGrid-Topic-Subscription url/runtime/webhooks/EventGrid?functionName=EventGridTriggerCustom

# Notes
1. Started from Microsoft's Todo sample api (https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
2. This sample uses Entity Framework Core in-memory DbContext by default, so restarting the app clears the DB
3. This solution provides a possible starting template for building service apis and is not production-ready (in-memory database, no authentication, etc)
4. Package.Infrastructure projects are meant to reside in a nuget package feed, but for simplicity and portability of this sample, the source projects are included and referenced directly
5. Package.Infrastructure.Storage & Functions.FunctionBlobTrigger - install and run latest azurite storage emulator
6. Package.Infrastructure.CosmosDb - install and run latest CosmosDB emulator
7. Azure Storage Explorer is usefull for blobs/queues/tables
8. Tunneling for EventGrid Functions - use VS Dev Tunnels or ngrok/tunnel to https://localhost:port IIS express
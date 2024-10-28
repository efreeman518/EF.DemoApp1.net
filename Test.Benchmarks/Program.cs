// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Test.Benchmarks;

//https://github.com/dotnet/BenchmarkDotNet

Console.WriteLine("Benchmarks");

//BenchmarkRunner.Run(new[] { typeof(RepositoryBenchmarks), typeof(RulesBenchmarks), typeof(ValidatorBenchmarks), typeof(TodoItemBenchmarks) });
BenchmarkRunner.Run([typeof(RulesBenchmarks), typeof(ValidatorBenchmarks), typeof(TodoItemBenchmarks)]);

//set up infrastructure for benchmark classes not using DbIntegrationTestBase/TestContainer
//[GlobalSetup]
//public void GlobalSetup()
//{
//    ServiceCollection services = [];
//    //bootstrapper service registrations - infrastructure, domain, application 
//    services
//        .RegisterInfrastructureServices(Config)
//        .RegisterBackgroundServices(Config)
//        .RegisterDomainServices(Config)
//        .RegisterApplicationServices(Config);
//    services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
//    _serviceScope = Services.CreateScope(); //needed for injecting scoped services
//}
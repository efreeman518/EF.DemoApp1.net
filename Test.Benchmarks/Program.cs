// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Test.Benchmarks;

Console.WriteLine("Benchmarks");
IConfigurationRoot config = Utility.Config;

//DI
Utility.GetServiceCollection().AddApplicationInsightsTelemetryWorkerService(config);

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().AddDebug().AddApplicationInsights();
});
Utility.GetServiceCollection().AddSingleton(loggerFactory);

//logging
ILogger<Program> logger = Utility.GetServiceProvider().GetRequiredService<ILogger<Program>>();
logger.Log(LogLevel.Information, "Benchmark Run Starting.");

BenchmarkRunner.Run(new[] { typeof(RepositoryBenchmarks), typeof(RulesBenchmarks), typeof(ValidatorBenchmarks), typeof(TodoItemBenchmarks) });

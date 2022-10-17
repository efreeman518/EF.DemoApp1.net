// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Test.Benchmarks;

Console.WriteLine("Benchmarks");
IConfigurationRoot config = Utility.Config;

//DI
Utility.Services.AddApplicationInsightsTelemetryWorkerService(config);
Utility.Services.AddLogging(configure => configure.AddConsole().AddDebug().AddApplicationInsights());

//logging
ILogger<Program> logger = Utility.GetServiceProvider().GetRequiredService<ILogger<Program>>();
logger.Log(LogLevel.Information, "Benchmark Run Starting.");

var summary = BenchmarkRunner.Run<RulesBenchmarks>();
Console.ReadKey();



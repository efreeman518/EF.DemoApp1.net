using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Package.Infrastructure.Test.Benchmarks.Scenarios;

namespace Package.Infrastructure.Test.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(HtmlExporter.Default)
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core90))
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

        Console.WriteLine("Running benchmarks for background task implementations...");

        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--debug":
                    // Run only one test for debugging
                    var debugSummary = BenchmarkRunner.Run<BackgroundTaskQueueBenchmarks>(config.WithOption(ConfigOptions.JoinSummary, true));
                    Console.WriteLine("Benchmark run completed. Press any key to exit.");
                    Console.ReadKey();
                    break;

                case "--throughput":
                    // Run high throughput scenario
                    var throughputSummary = BenchmarkRunner.Run<HighThroughputScenario>(config.WithOption(ConfigOptions.JoinSummary, true));
                    Console.WriteLine("Throughput benchmark completed. Press any key to exit.");
                    Console.ReadKey();
                    break;

                case "--concurrency":
                    // Run high concurrency scenario
                    var concurrencySummary = BenchmarkRunner.Run<HighConcurrencyScenario>(config.WithOption(ConfigOptions.JoinSummary, true));
                    Console.WriteLine("Concurrency benchmark completed. Press any key to exit.");
                    Console.ReadKey();
                    break;

                case "--channel":
                    // Run channel benchmark
                    var channelSummary = BenchmarkRunner.Run<ChannelBackgroundTaskQueueBenchmarks>(config.WithOption(ConfigOptions.JoinSummary, true));
                    Console.WriteLine("Channel benchmark completed. Press any key to exit.");
                    Console.ReadKey();
                    break;

                case "--all":
                    // Explicitly run all benchmarks
                    RunAllBenchmarks(config);
                    break;

                case "--help":
                    ShowHelp();
                    break;

                default:
                    Console.WriteLine($"Unknown option: {args[0]}");
                    ShowHelp();
                    break;
            }
        }
        else
        {
            // No arguments provided, run all benchmarks
            RunAllBenchmarks(config);
        }
    }

    private static void RunAllBenchmarks(IConfig config)
    {
        // Run all benchmarks in sequence with detailed reporting
        Console.WriteLine("Running all benchmark scenarios...");

        Console.WriteLine("\n1. Basic Queue Benchmarks:");
        var basicSummary = BenchmarkRunner.Run<BackgroundTaskQueueBenchmarks>(config);

        Console.WriteLine("\n2. Channel Queue Configuration Benchmarks:");
        var channelSummary = BenchmarkRunner.Run<ChannelBackgroundTaskQueueBenchmarks>(config);

        Console.WriteLine("\n3. High Concurrency Scenario:");
        var concurrencySummary = BenchmarkRunner.Run<HighConcurrencyScenario>(config);

        Console.WriteLine("\n4. High Throughput Scenario:");
        var throughputSummary = BenchmarkRunner.Run<HighThroughputScenario>(config);

        Console.WriteLine("\nAll benchmarks completed. Press any key to exit.");
        Console.ReadKey();
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Background Task Queue Benchmark Options:");
        Console.WriteLine("  --all         Run all benchmark scenarios (default if no option specified)");
        Console.WriteLine("  --debug       Run basic queue benchmarks with simplified settings for debugging");
        Console.WriteLine("  --channel     Run channel configuration benchmarks");
        Console.WriteLine("  --concurrency Run high concurrency scenario benchmarks");
        Console.WriteLine("  --throughput  Run high throughput scenario benchmarks");
        Console.WriteLine("  --help        Show this help message");
        Console.WriteLine("\nExample: dotnet run -c Release -- --all");

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}

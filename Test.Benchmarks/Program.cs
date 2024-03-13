// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Test.Benchmarks;

//https://github.com/dotnet/BenchmarkDotNet

Console.WriteLine("Benchmarks");

BenchmarkRunner.Run(new[] { typeof(RepositoryBenchmarks), typeof(RulesBenchmarks), typeof(ValidatorBenchmarks), typeof(TodoItemBenchmarks) });

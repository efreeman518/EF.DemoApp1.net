// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Test.Benchmarks;

Console.WriteLine("Benchmarks");

BenchmarkRunner.Run(new[] { typeof(RepositoryBenchmarks), typeof(RulesBenchmarks), typeof(ValidatorBenchmarks), typeof(TodoItemBenchmarks) });

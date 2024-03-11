using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Model;

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TodoItemBenchmarks
{
    [Params(5, 10)]
    public int NameLength { get; set; }

    TodoItem _todoItemDto = null!;

    [IterationSetup]
    public void Setup()
    {
        _todoItemDto = new TodoItem($"a{Support.Utility.RandomString(NameLength)}") { CreatedBy = "Test.Benchmarks" };
    }

    [Benchmark]
    public bool TodoItemValidation()
    {
        return _todoItemDto.Validate();
    }
}

﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Model;
using Package.Infrastructure.Domain.Contracts;

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
        _todoItemDto = new TodoItem($"a{Support.Utility.RandomString(NameLength)}");
    }

    [Benchmark]
    public DomainResult MethodTodoItemValidation()
    {
        return _todoItemDto.Validate();
    }
}

using BenchmarkDotNet.Attributes;
using Domain.Model;
using Domain.Rules;

namespace Test.Benchmarks;

[MemoryDiagnoser]
public class RulesBenchmarks
{
    [Params(5, 10)]
    public int NameLength { get; set; }

    TodoItem _todoItemDto = null!;
    string _regexMatch = null!;

    [IterationSetup]
    public void Setup()
    {
        _todoItemDto = new TodoItem { Name = Utility.RandomString(NameLength) };
        _regexMatch = $"{_todoItemDto.Name.Substring(0, 1)}.*{_todoItemDto.Name.Substring(_todoItemDto.Name.Length - 3)}";
    }

    [Benchmark]
    public bool TodoNameLengthRule()
    {
        return new TodoNameLengthRule(NameLength).IsSatisfiedBy(_todoItemDto);
    }

    [Benchmark]
    public bool TodoNameRegexRule()
    {
        return new TodoNameRegexRule(_regexMatch).IsSatisfiedBy(_todoItemDto);
    }

    [Benchmark]
    public bool CompositeRule()
    {
        return new TodoCompositeRule(NameLength, _regexMatch).IsSatisfiedBy(_todoItemDto);
    }

}

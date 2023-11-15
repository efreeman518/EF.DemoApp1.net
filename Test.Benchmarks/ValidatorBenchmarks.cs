using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Common;

//https://github.com/dotnet/BenchmarkDotNet

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ValidatorBenchmarks : IDisposable
{
    private IServiceScope _serviceScope = null!;
    private IValidationHelper _validationHelper = null!;
    TodoItemDto _todoItemDto = null!;

    //[Params(5, 10)]
    //public int NameLength { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _serviceScope = Utility.GetServiceProvider().CreateScope();
        _validationHelper = _serviceScope.ServiceProvider.GetRequiredService<IValidationHelper>();
        _todoItemDto = new TodoItemDto { Name = Guid.NewGuid().ToString() };
    }

    [IterationCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Cleanup
        _serviceScope?.Dispose();
    }

    [Benchmark]
    public async Task<FluentValidation.Results.ValidationResult?> TodoItemValidateAsync()
    {
        try
        {
            return await _validationHelper.ValidateAsync(_todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
            return null;
        }
    }

    [Benchmark]
    public async Task TodoItemValidateAndThrowAsync()
    {
        try
        {
            await _validationHelper.ValidateAndThrowAsync(_todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
        }
    }
}

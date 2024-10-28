using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Shared.Enums;
using FluentValidation;
using SampleApp.Support.Validators;

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ValidatorBenchmarks
{
    //[Params(5, 10)]
    //public int NameLength { get; set; }

    [Benchmark]
    public static FluentValidation.Results.ValidationResult? ValidatorTodoItemValidate()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            return new TodoItemDtoValidator().Validate(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
            return null;
        }
    }

    public static void ValidatorTodoItemValidateAndThrow()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            new TodoItemDtoValidator().ValidateAndThrow(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
        }
    }

    [Benchmark]
    public static async Task<FluentValidation.Results.ValidationResult?> ValidatorTodoItemValidateAsync()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            return await (new TodoItemDtoValidator()).ValidateAsync(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
            return null;
        }
    }

    [Benchmark]
    public static async Task ValidatorTodoItemValidateAndThrowAsyncAsync()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            await (new TodoItemDtoValidator()).ValidateAndThrowAsync(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
        }
    }
}

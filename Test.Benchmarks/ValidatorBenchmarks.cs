using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common;
using SampleApp.Bootstrapper;
using Test.Support;

//https://github.com/dotnet/BenchmarkDotNet

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ValidatorBenchmarks
{
    protected readonly static IConfigurationRoot Config = Utility.BuildConfiguration().AddUserSecrets<ValidatorBenchmarks>().Build();
    private readonly IServiceScope _serviceScope = null!;
    private readonly IValidationHelper _validationHelper = null!;
    private readonly IServiceProvider _services = null!;

    protected IServiceProvider Services => _services;

    public ValidatorBenchmarks()
    {
        ServiceCollection services = [];
        //bootstrapper service registrations - infrastructure, domain, application 
        services
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        _services = services.BuildServiceProvider(validateScopes: true);
        _serviceScope = _services.CreateScope(); //needed for injecting scoped services
        _validationHelper = _serviceScope.ServiceProvider.GetRequiredService<IValidationHelper>();
    }

    //[Params(5, 10)]
    //public int NameLength { get; set; }

    [Benchmark]
    public async Task<FluentValidation.Results.ValidationResult?> TodoItemValidateAsync()
    {
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            return await _validationHelper.ValidateAsync(todoItemDto);
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
        var todoItemDto = new TodoItemDto(null, Guid.NewGuid().ToString(), TodoItemStatus.Created);
        try
        {
            await _validationHelper.ValidateAndThrowAsync(todoItemDto);
        }
        catch (Exception)
        {
            //ignore for benchmark test
        }
    }

    //BenchmarkDotNet does not support async setup/teardown
    //https://github.com/dotnet/BenchmarkDotNet/issues/1738#issuecomment-1687832731
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceScope.Dispose();
    }
}

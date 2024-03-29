using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Model;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Common.Contracts;
using Test.Support;

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RepositoryBenchmarks : DbIntegrationTestBase
{
    private ITodoRepositoryQuery _repo = null!;

    [Benchmark]
    public async Task Repo_SearchTodoItemAsync()
    {
        var search = new SearchRequest<TodoItemSearchFilter> { PageSize = 10, PageIndex = 1 };
        _ = await _repo.SearchTodoItemAsync(search);
    }

    [Benchmark]
    public async Task Repo_GetPageEntitiesAsync()
    {
        _ = await _repo.QueryPageAsync<TodoItem>(pageSize: 10, pageIndex: 1);
    }

    //BenchmarkDotNet does not support async setup/teardown
    //https://github.com/dotnet/BenchmarkDotNet/issues/1738#issuecomment-1687832731

    /// <summary>
    /// Reset & reseed DB here to avoid the overhead of resetting the database inside the benchmark test
    /// </summary>
    [IterationSetup]
    public static void IterationSetup()
    {
        List<Action> seedFactories = [() => DbContext.SeedEntityData()];
        List<string>? seedPaths = [TestConfigSection.GetValue<string>("SeedFilePath")];
        ResetDatabaseAsync(true, seedPaths: seedPaths, seedFactories: seedFactories).GetAwaiter().GetResult(); //no async support
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        ConfigureTestInstanceAsync(nameof(RepositoryBenchmarks)).GetAwaiter().GetResult(); //no async support
        _repo = (TodoRepositoryQuery)ServiceScope.ServiceProvider.GetRequiredService(typeof(ITodoRepositoryQuery));
    }

    [GlobalCleanup]
    public static void GlobalCleanup()
    {
        BaseClassCleanup().GetAwaiter().GetResult(); //no async support
    }
}

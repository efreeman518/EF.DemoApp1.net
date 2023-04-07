using Application.Contracts.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Model;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data.Contracts;
using Test.Support;

//https://github.com/dotnet/BenchmarkDotNet

namespace Test.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RepositoryBenchmarks
{
    //Infrastructure
    private readonly ITodoRepositoryQuery _repo;

    public RepositoryBenchmarks()
    {
        var mapper = SampleApp.Bootstrapper.Automapper.ConfigureAutomapper.CreateMapper();
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities =>
            {
                //custom data scenario that default seed data does not cover
                entities.Add(new TodoItem("a entity"));
            })
            .Build<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        _repo = new TodoRepositoryQuery(db, src, mapper);
    }

    [IterationSetup]
    public void Setup()
    {
        _ = GetHashCode();
    }

    [Benchmark]
    public async Task Repo_SearchTodoItemAsync()
    {
        var search = new SearchRequest<TodoItemSearchFilter> { PageSize = 10, PageIndex = 1 };
        _ = await _repo.SearchTodoItemAsync(search);
    }

    [Benchmark]
    public async Task Repo_GetPageEntitiesAsync()
    {
        _ = await _repo.GetPageEntitiesAsync<TodoItem>(pageSize: 10, pageIndex: 1);
    }


}

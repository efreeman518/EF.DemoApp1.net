using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Domain.Model;
using Infrastructure.Data;
using Infrastructure.Repositories;
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
                entities.Add(new TodoItem { Id = Guid.NewGuid(), Name = "some entity", CreatedBy = "unit test", UpdatedBy = "unit test", CreatedDate = DateTime.UtcNow });
            })
            .GetOrBuild<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        _repo = new TodoRepositoryQuery(db, audit, mapper);
    }

    [IterationSetup]
    public void Setup()
    {
        _ = GetHashCode();
    }

    [Benchmark]
    public async Task Repo_PagedTodoDtoResponse()
    {
        _ = await _repo.GetPageTodoItemDtoAsync(10, 1);
    }

    [Benchmark]
    public async Task Repo_PagedTodoResponse()
    {
        _ = await _repo.GetPageEntityAsync<TodoItem>(pageSize: 10, pageIndex: 1);
    }


}

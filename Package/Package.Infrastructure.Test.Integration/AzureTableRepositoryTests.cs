using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Table;
using Package.Infrastructure.Test.Integration.Model;
using Package.Infrastructure.Test.Integration.Table;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Table account required - Azurite storage emulator, Azure Storage, CosmosDB emulator or CosomsDB.")]

[TestClass]
public class AzureTableRepositoryTests : IntegrationTestBase
{
    readonly ITableRepository1 _repo;

    public AzureTableRepositoryTests() : base()
    {
        _repo = (ITableRepository1)Services.GetRequiredService(typeof(ITableRepository1));
    }

    [TestMethod]
    public async Task Todo_crud_pass()
    {
        //Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        //Create entity
        TodoItemTableEntity? todo = new(name: "item1-a");

        //create
        var responseCode = await _repo.CreateItemAsync(todo);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(todo.PartitionKey, todo.RowKey);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Created, todo.Status);

        //update
        todo.SetStatus(TodoItemStatus.Completed);
        responseCode = await _repo.UpdateItemAsync(todo, TableUpdateMode.Replace);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(todo.PartitionKey, todo.RowKey);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Completed, todo.Status);

        //delete & validate
        await _repo.DeleteItemAsync<TodoItemTableEntity>(todo.PartitionKey, todo.RowKey);
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(todo.PartitionKey, todo.RowKey);
        Assert.IsNull(todo);

        //delete table
        responseCode = await _repo.DeleteTableAsync(tableName);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);
    }

    [TestMethod]
    public async Task Page_linq_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        List<TodoItemTableEntity> fullList = [];
        IReadOnlyList<TodoItemTableEntity>? todos;
        int pageSize = 10;

        //LINQ - page with filter
        Expression<Func<TodoItemTableEntity, bool>> filterLinq;
        //filterLinq = t => t.IsComplete;
        //filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString()); //Table SDK filter by enum is ugly atm
        filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString());
        string? continuationToken = null;


        do
        {
            (todos, continuationToken) = await _repo.QueryPageAsync(continuationToken, pageSize, filterLinq);
            Assert.IsNotNull(todos);
            fullList.AddRange(todos);
        }
        while (continuationToken != null);

        Assert.IsNotNull(fullList);
    }

    [TestMethod]
    public async Task Page_odata_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        List<TodoItemTableEntity> fullList = [];
        IReadOnlyList<TodoItemTableEntity>? todos;
        int pageSize = 10;

        //filterOData = "IsComplete eq true";
        string filterOData = "Status eq 'Completed'";
        string? continuationToken = null;

        do
        {
            (todos, continuationToken) = await _repo.QueryPageAsync<TodoItemTableEntity>(continuationToken, pageSize, null, filterOData);
            Assert.IsNotNull(todos);
            fullList.AddRange(todos);
        }
        while (continuationToken != null);
        Assert.IsNotNull(fullList);
    }

    [TestMethod]
    public async Task Stream_linq_batch_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        //LINQ - page with filter
        Expression<Func<TodoItemTableEntity, bool>> filterLinq;
        //filterLinq = t => t.IsComplete;
        //filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString()); //Table SDK filter by enum is ugly atm
        filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString());
        var batchSize = 10;

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-linq-batch");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream(filterLinq, null).ConcurrentBatchAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some async I/O work
            await Task.Delay(1000);
            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, batchSize);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-linq-batch Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    [TestMethod]
    public async Task Stream_odata_batch_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        string filterOData = "Status eq 'Completed'";
        var batchSize = 10;

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-odata-batch");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream<TodoItemTableEntity>(null, filterOData, null).ConcurrentBatchAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some async I/O work
            await Task.Delay(1000);
            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, batchSize);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-odata-batch Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    [TestMethod]
    public async Task Stream_linq_pipe_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        //LINQ - page with filter
        Expression<Func<TodoItemTableEntity, bool>> filterLinq;
        //filterLinq = t => t.IsComplete;
        //filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString()); //Table SDK filter by enum is ugly atm
        filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString());
        var maxConcurrent = 10;

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-linq-pipe");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream(filterLinq, null).ConcurrentPipeAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some async I/O work
            await Task.Delay(1000);
            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxConcurrent);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-linq-pipe Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    [TestMethod]
    public async Task Stream_odata_pipe_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        string filterOData = "Status eq 'Completed'";
        var maxConcurrent = 10;

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-odata-pipe");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream<TodoItemTableEntity>(null, filterOData, null).ConcurrentPipeAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some async I/O work
            await Task.Delay(1000);
            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxConcurrent);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-odata-pipe Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    [TestMethod]
    public async Task Stream_odata_parallel_async_work_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        string filterOData = "Status eq 'Completed'";

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-parallel-async");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream<TodoItemTableEntity>(null, filterOData, null).ProcessParallelAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some CPU async work
            await Task.Delay(1000);
            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, -1);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-parallel-async Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    [TestMethod]
    public async Task Stream_odata_parallel_sync_work_pass()
    {
        //Get/Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableName);

        await PopulateTableData();

        //threadsafe collection on the client side
        ConcurrentBag<TodoItemTableEntity> fullList = [];

        string filterOData = "Status eq 'Completed'";

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start stream-parallel-sync");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //stream keeps the pipe full
        var total = await _repo.GetStream<TodoItemTableEntity>(null, filterOData, null).ProcessParallelSync((item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");

            //do some CPU sync/blocking work
#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
            Thread.Sleep(1000);
#pragma warning restore S2925 // "Thread.Sleep" should not be used in tests

            fullList.Add(item);

            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, -1);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{DateTime.Now} - Finish stream-parallel-sync Total:{total} ElapsedMS:{elapsed_time}");

        Assert.AreEqual(total, fullList.Count);
    }

    private async Task PopulateTableData(int numRows = 100)
    {
        //populate table with docs (random Status)
        Array statuses = Enum.GetValues<TodoItemStatus>();
        Random random = new();
        for (var i = 0; i < numRows; i++)
        {
            TodoItemStatus status = (TodoItemStatus)(statuses.GetValue(random.Next(statuses.Length)) ?? TodoItemStatus.Created);
            TodoItemTableEntity todo1 = new(status: status); //TableEntity - PartitionKey, RowKey created on instantiation 
            await _repo.CreateItemAsync(todo1);
        }
    }
}



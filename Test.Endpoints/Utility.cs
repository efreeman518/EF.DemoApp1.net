using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Test.Support;

namespace Test.Endpoints;

/// <summary>
/// Uses WebApplicationFactory to create a test http service which can be hit using httpclient
/// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
/// </summary>
public static class Utility
{
    private static IConfigurationRoot? _config = null;
    private static readonly ConcurrentDictionary<string, IDisposable> _factories = new();

    public static IConfigurationRoot GetConfiguration()
    {
        if (_config != null) return _config;

        //order matters here (last wins)
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettingsPre.json", true) //test project settings
            .AddJsonFile("appsettings.json", false); //from api

        IConfigurationRoot config = builder.Build();
        string env = config.GetValue<string>("Environment", "development")!;
        var isDevelopment = env?.ToLower() == "development";
        if (env != null)
        {
            builder.AddJsonFile($"appsettings.{env}.json", true);
        }
        builder
            .AddJsonFile("appsettingsPost.json", true) //test project override any api settings
            .AddEnvironmentVariables(); //pipeline can override settings

        if (isDevelopment) builder.AddUserSecrets<EndpointTestBase>();
        _config = builder.Build();

        return _config;
    }

    public static HttpClient GetClient<TEntryPoint>(bool allowAutoRedirect = true, string baseAddress = "http://localhost")
        where TEntryPoint : class
    {
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowAutoRedirect, //default = true; set to false for testing app's first response being a redirect with Location header
            BaseAddress = new Uri(baseAddress) //default = http://localhost
        };
        var factory = GetFactory<TEntryPoint>(); //must live for duration of the client
        HttpClient client = factory.CreateClient(options);

        return client;
    }

    private static WebApplicationFactory<TEntryPoint> GetFactory<TEntryPoint>()
        where TEntryPoint : class
    {
        string key = typeof(TEntryPoint).FullName!;
        if (_factories.TryGetValue(key, out var result)) return (WebApplicationFactory<TEntryPoint>)result;
        var factory = new CustomWebApplicationFactory<TEntryPoint>(); //must live for duration of the client
        _factories.TryAdd(key, factory); //hold for subsequent use
        return factory;
    }

    ///// <summary>
    ///// Returns searchable IHtmlDocument
    ///// </summary>
    ///// <param name="response"></param>
    ///// <returns></returns>
    //public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response)
    //{
    //    var content = await response.Content.ReadAsStringAsync();
    //    var document = await BrowsingContext.New().OpenAsync(ResponseFactory, CancellationToken.None);
    //    return (IHtmlDocument)document;

    //    void ResponseFactory(VirtualResponse htmlResponse)
    //    {
    //        htmlResponse
    //            .Address(response.RequestMessage!.RequestUri)
    //            .Status(response.StatusCode);

    //        MapHeaders(response.Headers);
    //        MapHeaders(response.Content.Headers);

    //        htmlResponse.Content(content);

    //        void MapHeaders(HttpHeaders headers)
    //        {
    //            foreach (var header in headers)
    //            {
    //                foreach (var value in header.Value)
    //                {
    //                    htmlResponse.Header(header.Key, value);
    //                }
    //            }
    //        }
    //    }
    //}

    #region InMemory DbContext

    //InMemory is like static, not created with each new DbContext, stays in memory for subsequent DbContexts for a given name
    //public static TodoDbContextTrxn SetupInMemoryDB(string uniqueName, bool seed)
    //{
    //    DbContextOptions<TodoDbContextTrxn> options = new DbContextOptionsBuilder<TodoDbContextTrxn>().UseInMemoryDatabase(uniqueName).Options;
    //    TodoDbContextTrxn db = new(options);
    //    if (seed) SeedInMemoryDB(db);
    //    return db;
    //}

    //public static void SeedInMemoryDB(TodoDbContextTrxn db)
    //{
    //    db.TodoItems.AddRange(GetSeedData());
    //    db.SaveChanges();
    //}

    //public static void ReseedInMemoryDB(TodoDbContextTrxn db)
    //{
    //    db.TodoItems.RemoveRange(db.TodoItems);
    //    SeedInMemoryDB(db);
    //}

    //public static List<TodoItem> GetSeedData()
    //{
    //    return new InMemoryDbBuilder().SeedDefaultEntityData()
    //}

    //private static void Upsert(TodoDbContextTrxn db, TodoItem item)
    //{
    //    if (!db.Set<TodoItem>().Any(td => td.Id == item.Id)) db.Add(item);
    //}

    #endregion

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factories.ToList().ForEach(f => (f.Value).Dispose());
    }
}

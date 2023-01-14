using Application.Contracts.Model;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Plugins.Network.Ping;
using Package.Infrastructure.Data.Contracts;


//https://nbomber.com/

namespace Test.Load;

internal static class TodoLoadTest
{
    public static void Run(string baseUrl)
    {
        var httpFactory = ClientFactory.Create(
            name: "http_factory",
            clientCount: 1,
            initClient: (number, context) => Task.FromResult(new HttpClient())
        );

        var steps = new[]
        {
            Utility.CreateStep<object,PagedResponse<TodoItemDto>>(httpFactory,"getpage", $"{baseUrl}/api/TodoItems", HttpMethod.Get),
            Utility.CreateStep<TodoItemDto, TodoItemDto>(httpFactory,"post", $"{baseUrl}/api/TodoItems", HttpMethod.Post, null,
                //assemble payload for the request
                (context) =>
                {
                    return new TodoItemDto {Name = $"a{Guid.NewGuid()}" };
                }),
            Utility.CreateStep <object, TodoItemDto>(httpFactory,"get", $"{baseUrl}/api/TodoItems/", HttpMethod.Get, 
                //assemble url for the request from previous response
                (context) =>
                {
                    var todoItem =  (TodoItemDto)context.Data["post"];
                    return todoItem.Id.ToString();
                }),
            Utility.CreateStep<TodoItemDto, TodoItemDto>(httpFactory,"put", $"{baseUrl}/api/TodoItems/", HttpMethod.Put,
                //assemble querystring for the request from previous response
                (context) =>
                {
                    var todoItem =  (TodoItemDto)context.Data["get"];
                    return todoItem.Id.ToString();
                },
                //assemble payload for the request from previous response
                (context) =>
                {
                    var todoItem =  (TodoItemDto)context.Data["get"];
                    return new TodoItemDto { Id = todoItem.Id, Name = "some updated name"};
                }),
        };

        var scenario = ScenarioBuilder
            .CreateScenario("todo-crud", steps)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                //Simulation.KeepConstant(10, during: TimeSpan.FromSeconds(30))
                Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(30))
            );

        // creates ping plugin that brings additional reporting data
        var pingPluginConfig = PingPluginConfig.CreateDefault(new[] { "localhost" });
        var pingPlugin = new PingPlugin(pingPluginConfig);

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(pingPlugin)
            .WithReportFormats(ReportFormat.Html)
            .Run();
    }
}

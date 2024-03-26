using Application.Contracts.Model;
using Domain.Shared.Enums;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Plugins.Network.Ping;
using Package.Infrastructure.Common.Contracts;

//https://nbomber.com/

namespace Test.Load;

internal static class TodoLoadTest
{
    public static void Run(string baseUrl)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(baseUrl);

        var scenario = Scenario.Create("todo-crud", async context =>
            {
                // you can define and execute any logic here,
                // for example: send http request, SQL query etc
                // NBomber will measure how much time it takes to execute your logic

                await Utility.RunStep<object, PagedResponse<TodoItemDto>>(context, httpClient, "getpage", HttpMethod.Get, $"{baseUrl}/api/v1.1/TodoItems");
                await Utility.RunStep<TodoItemDto, TodoItemDto>(context, httpClient, "post", HttpMethod.Post, $"{baseUrl}/api/v1.1/TodoItems", null,
                    // assemble the payload for this step request 
                    (context) =>
                    {
                        var name = $"a{Guid.NewGuid()}";
                        return new TodoItemDto(null, name, TodoItemStatus.Created, name, name);
                    });
                await Utility.RunStep<object, TodoItemDto>(context, httpClient, "get", HttpMethod.Get, $"{baseUrl}/api/v1.1/TodoItems",
                    //assemble the url for this step request using previous step response
                    (context) =>
                    {
                        var todoItem = (TodoItemDto)context.Data["post"];
                        return $"/{todoItem.Id}";
                    });
                await Utility.RunStep<object, TodoItemDto>(context, httpClient, "put", HttpMethod.Put, $"{baseUrl}/api/v1.1/TodoItems",
                    //assemble the url for this step request using previous step response
                    (context) =>
                    {
                        var todoItem = (TodoItemDto)context.Data["get"];
                        return $"/{todoItem.Id}";
                    },
                    // assemble the payload for this step request using previous step response
                    (context) =>
                    {
                        var todoItem = (TodoItemDto)context.Data["get"];
                        return new TodoItemDto(todoItem.Id, $"updated {todoItem.Name}", TodoItemStatus.Created);
                    });

                return Response.Ok();
            })

            //debug single requestor
            //.WithoutWarmUp()
            //.WithLoadSimulations(Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(10)));

            //normal load
            .WithWarmUpDuration(TimeSpan.FromSeconds(20))
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 5,
                             interval: TimeSpan.FromSeconds(1),
                             during: TimeSpan.FromSeconds(30)),
                Simulation.Inject(rate: 10,
                      interval: TimeSpan.FromSeconds(1),
                      during: TimeSpan.FromSeconds(30))
            );


        // creates ping plugin that brings additional reporting data
        var pingPluginConfig = PingPluginConfig.CreateDefault("localhost");
        var pingPlugin = new PingPlugin(pingPluginConfig);

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(pingPlugin)
            .WithReportFormats(ReportFormat.Html)
            .Run();
    }
}

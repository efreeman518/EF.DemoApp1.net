using Application.Contracts.Model;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Plugins.Network.Ping;
using System.Text.Json;

namespace Test.Load;

internal static class TodoLoadTest
{
    public static void Run()
    {
        var httpFactory = ClientFactory.Create(
            name: "http_factory",
            clientCount: 1,
            initClient: (number, context) => Task.FromResult(new HttpClient())
        );

        var steps = new[]
        {
            Support.CreateStep(httpFactory,"getpage", "api/TodoItems", HttpMethod.Get),
            Support.CreateStep(httpFactory,"post", "api/TodoItems", HttpMethod.Post, genPayload()),
        };

        var scenario = ScenarioBuilder
            .CreateScenario("addEntry", steps)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.KeepConstant(10, during: TimeSpan.FromSeconds(30))
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

    private static string genPayload(Guid? id = null)
    {
        var dto = new TodoItemDto { Name = Guid.NewGuid().ToString() };
        if (id != null) dto.Id = (Guid)id;
        return JsonSerializer.Serialize(dto);
    }
}

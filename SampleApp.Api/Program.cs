using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SampleApp.Api;

public class Program
{
    protected Program() { }

    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureLogging(ConfigureLogger);
                webBuilder.UseStartup<Startup>();
            });

    static void ConfigureLogger(WebHostBuilderContext hostingContext, ILoggingBuilder logging)
    {
        logging.AddApplicationInsights();

        if (hostingContext.HostingEnvironment.IsDevelopment())
        {
            logging.AddDebug();
            logging.AddConsole();
        }
    }
}

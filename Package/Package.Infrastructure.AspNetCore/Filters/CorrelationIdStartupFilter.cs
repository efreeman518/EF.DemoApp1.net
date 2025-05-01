using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Package.Infrastructure.AspNetCore.Filters;

/// <summary>
/// Convention to add a correlation ID to the request header if it does not exist.
/// </summary>
public class CorrelationIdStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
                {
                    context.Request.Headers["X-Correlation-ID"] = Guid.NewGuid().ToString();
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}

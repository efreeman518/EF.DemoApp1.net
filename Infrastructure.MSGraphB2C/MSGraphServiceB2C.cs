using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Package.Infrastructure.MSGraph;

namespace Infrastructure.MSGraphB2C;

/// <summary>
/// Implementation for specific GraphServiceClient keyed "MSGraphService1"
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="graphClient"></param>
public class MSGraphServiceB2C(ILogger<MSGraphServiceB2C> logger, IOptions<MSGraphServiceB2CSettings> settings, [FromKeyedServices("MSGraphServiceB2C")] GraphServiceClient graphClient)
    : MSGraphServiceBase(logger, settings, graphClient), IMSGraphServiceB2C
{

}

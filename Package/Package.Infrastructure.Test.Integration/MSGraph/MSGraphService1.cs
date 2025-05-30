using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Package.Infrastructure.MSGraph;

namespace Package.Infrastructure.Test.Integration.MSGraph;

/// <summary>
/// Implementation for specific GraphServiceClient keyed "MSGraphService1"
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="graphClient"></param>
public class MSGraphService1(ILogger<MSGraphServiceBase> logger, IOptions<MSGraphServiceSettings1> settings, [FromKeyedServices("MSGraphService1")] GraphServiceClient graphClient) 
    : MSGraphServiceBase(logger, settings, graphClient), IMSGraphService1
{

}

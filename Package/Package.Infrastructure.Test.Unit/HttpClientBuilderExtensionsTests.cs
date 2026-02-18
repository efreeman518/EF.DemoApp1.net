using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Utility.UI;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class HttpClientBuilderExtensionsTests
{
    [TestMethod]
    public void AddCustomResilience_registers_pipeline_with_greenfield_signature()
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("greenfield-http-client");
        var excluded = new List<int> { 400, 404 };

        IReadOnlyCollection<int> readonlyExcluded = excluded;
        var configured = builder.AddCustomResilience(
            pipelineName: "new-pipeline",
            excludedStatusCodes: readonlyExcluded,
            maxRetryAttempts: 2,
            attemptTimeoutInSeconds: 1,
            totalTimeoutSeconds: 5);

        Assert.AreSame(builder, configured);
    }
}

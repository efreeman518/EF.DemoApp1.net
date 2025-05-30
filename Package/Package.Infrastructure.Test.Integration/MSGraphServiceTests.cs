using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.MSGraph.Models;
using Package.Infrastructure.Test.Integration.MSGraph;
using System.Text.Json;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("MSGraph requires some Azure tenant - EntraID or AzureB2C")]

[TestClass]
public class MSGraphServiceTests : IntegrationTestBase
{
    private readonly IMSGraphService1 _graph;

    public MSGraphServiceTests() : base()
    {
        _graph = Services.GetRequiredService<IMSGraphService1>();
    }

    [TestMethod]
    public async Task CreateUser_pass()
    {
        Guid userTenantId = Guid.NewGuid();
        var roles = new List<string> { "Admin", "User" };
        var additionalData = new Dictionary<string, object>
        {
            { "UserTenantId", userTenantId },
            { "UserRoles", JsonSerializer.Serialize(roles)}
        };

        var request = new UpsertUserRequest(null, true, "SomeDisplayName", "eben.freeman+a3@gmail.com", true, "changeOnLoginRequired8[", additionalData);
        var userId = await _graph.UpsertUserAsync(request);

        Assert.IsNotNull(userId, "User ID should not be null."); //eed180f4-dde1-4be3-8ae1-fff12c64a08a  8737ffb9-d4c3-4231-8882-2c67ef3eec7e

    }
}



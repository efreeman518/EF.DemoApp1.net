using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta.Models.ODataErrors;
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
    public async Task User_Create_Update_Delete_pass()
    {
        Random rnd = new();
        int randomInt = rnd.Next(1000, 10000);
        Guid userTenantId = Guid.NewGuid();

        var displayName = $"SomeDisplayName{randomInt}";
        var email = $"eben.freeman+{randomInt}@gmail.com";
        var roles = new List<string> { "StandardUser" };
        //var displayName = $"ef.admin";
        //var email = $"eben.freeman+cdadmin@gmail.com";
        //var roles = new List<string> { "GlobalAdmin" };

        var additionalData = new Dictionary<string, object>
        {
            { "UserTenantId", userTenantId },
            { "UserRoles", JsonSerializer.Serialize(roles)}
        };

        //new user
        var request = new GraphUserRequest(null, true, displayName, email, true, "change!On@Login%Required", additionalData);
        var userId = await _graph.CreateUserAsync(request);
        Assert.IsNotNull(userId, "User ID should not be null.");

        //get user & verify
        var user = await _graph.GetUserAsync(userId);
        Assert.IsNotNull(user, "User should not be null.");
        Assert.AreEqual(userId, user.Id); //eed180f4-dde1-4be3-8ae1-fff12c64a08a  8737ffb9-d4c3-4231-8882-2c67ef3eec7e
        Assert.AreEqual(displayName, user.DisplayName);
        Assert.AreEqual(request.Email.Split('@')[0], user.MailNickname);
        Assert.AreEqual(email, user.Identities!.First(i => i.SignInType == "emailAddress").IssuerAssignedId);
        Assert.AreEqual(userTenantId.ToString(), user.AdditionalData[user.AdditionalData.Keys.First(k => k.Contains("UserTenantId"))].ToString()); //this should remain unchanged
        Assert.AreEqual(JsonSerializer.Serialize(roles), user.AdditionalData[user.AdditionalData.Keys.First(k => k.Contains("UserRoles"))].ToString());

        //update user
        randomInt = rnd.Next(1000, 10000);
        displayName = $"UpdatedDisplayName{randomInt}";
        email = $"eben.freeman+update{randomInt}@gmail.com";
        roles = ["SpecialUser"];
        additionalData = new Dictionary<string, object>
        {
            { "UserRoles", JsonSerializer.Serialize(roles) }
        };

        request = new GraphUserRequest(userId, true, displayName, email, true, $"change!On@Login%Required*{email}", additionalData);
        await _graph.UpdateUserAsync(request);

        //get user & verify
        user = await _graph.GetUserAsync(userId);
        Assert.IsNotNull(user, "User should not be null.");
        Assert.AreEqual(userId, user.Id); //eed180f4-dde1-4be3-8ae1-fff12c64a08a  8737ffb9-d4c3-4231-8882-2c67ef3eec7e
        Assert.AreEqual(displayName, user.DisplayName);
        Assert.AreEqual(request.Email.Split('@')[0], user.MailNickname);
        Assert.AreEqual(userTenantId.ToString(), user.AdditionalData[user.AdditionalData.Keys.First(k => k.Contains("UserTenantId"))].ToString()); //this should remain unchanged
        Assert.AreEqual(JsonSerializer.Serialize(roles), user.AdditionalData[user.AdditionalData.Keys.First(k => k.Contains("UserRoles"))].ToString());

        //change email for identity - login
        await _graph.ChangeUserIdentityAsync(userId, email);
        user = await _graph.GetUserAsync(userId);
        Assert.IsNotNull(user, "User should not be null.");
        Assert.AreEqual(email, user.Identities!.First(i => i.SignInType == "emailAddress").IssuerAssignedId);

        //delete user
        await _graph.DeleteUserAsync(userId);
        await _graph.GetUserAsync(userId).ContinueWith(t =>
        {
            Assert.IsTrue(t.IsFaulted, "User should not be found after deletion.");
            Assert.IsInstanceOfType<ODataError>(t.Exception?.InnerException, "Expected ODataError for user not found.");
        });

    }
}



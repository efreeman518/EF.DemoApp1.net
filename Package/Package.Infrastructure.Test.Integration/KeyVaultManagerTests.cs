using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Test.Integration.KeyVault;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("Key Vault setup required.")]

[TestClass]
public class KeyVaultManagerTests : IntegrationTestBase
{
    private readonly IKeyVaultManager1 _vault;

    public KeyVaultManagerTests()
    {
        _vault = Services.GetRequiredService<IKeyVaultManager1>();
    }

    [TestMethod]
    public async Task Set_and_get_secret_pass()
    {
        var secretName = $"secret-{Guid.NewGuid()}";
        var secretValue = "some-secret-value";

        var saveSecretResponse = await _vault.SaveSecretAsync(secretName, secretValue);
        Assert.AreEqual(secretValue, saveSecretResponse);

        var getSecretResponse = await _vault.GetSecretAsync(secretName);
        Assert.AreEqual(secretValue , getSecretResponse);
    }
}

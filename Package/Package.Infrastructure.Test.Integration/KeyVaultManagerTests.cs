using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Test.Integration.KeyVault;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Key Vault setup required.")]

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

    [TestMethod]
    public async Task Create_and_get_key_pass()
    {
        var keyName = $"key-{Guid.NewGuid()}";

        var jwk = await _vault.CreateKeyAsync(keyName, KeyType.Rsa);
        Assert.IsNotNull(jwk);
        jwk = await _vault.GetKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        jwk = await _vault.DeleteKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        try
        {
            jwk = await _vault.GetKeyAsync(keyName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "KeyNotFound")
        {
            Assert.IsTrue(true);
        }
        
    }
}

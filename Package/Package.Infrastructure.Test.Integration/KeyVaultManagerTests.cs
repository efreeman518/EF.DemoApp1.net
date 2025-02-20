using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.KeyVault;
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
    public async Task CryptoUtilityEncryptDecrypt()
    {
        //var cryptoUtility = Services.GetRequiredervice<IKeyVaultCryptoUtility>();
        var cryptoUtility = Services.GetRequiredKeyedService<IKeyVaultCryptoUtility>("SomeCryptoUtil");
        var plainTest = "This is some sensitive data";
        var encrypted = await cryptoUtility.EncryptAsync(plainTest);
        Assert.IsTrue(encrypted.Length > 0);
        var decrypted = await cryptoUtility.DecryptAsync(encrypted);
        Assert.AreEqual(plainTest, decrypted);
    }

    [TestMethod]
    public async Task Secret_crud_pass()
    {
        var secretName = $"secret-{Guid.NewGuid()}";
        var secretValue = "some-secret-value";
        var secretValueUpdated = $"update {secretValue}";

        var response = await _vault.SaveSecretAsync(secretName, secretValue);
        Assert.AreEqual(secretValue, response);

        response = await _vault.GetSecretAsync(secretName);
        Assert.AreEqual(secretValue, response);

        response = await _vault.SaveSecretAsync(secretName, secretValueUpdated);
        Assert.AreEqual(secretValueUpdated, response);

        response = await _vault.GetSecretAsync(secretName);
        Assert.AreEqual(secretValueUpdated, response);

        response = (await _vault.StartDeleteSecretAsync(secretName)).Name;
        Assert.AreEqual(secretName, response);

        try
        {
            _ = await _vault.GetSecretAsync(secretName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            Assert.IsTrue(ex.ErrorCode == "SecretNotFound");
        }
    }

    [TestMethod]
    public async Task Key_crud_pass()
    {
        var keyName = $"key-{Guid.NewGuid()}";

        var jwk = await _vault.CreateKeyAsync(keyName, KeyType.Rsa);
        Assert.IsNotNull(jwk);
        jwk = await _vault.GetKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        jwk = await _vault.RotateKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        jwk = await _vault.DeleteKeyAsync(keyName);
        Assert.IsNotNull(jwk);
        try
        {
            _ = await _vault.GetKeyAsync(keyName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "KeyNotFound")
        {
            Assert.IsTrue(ex.ErrorCode == "KeyNotFound");
        }
    }

    [TestMethod]
    public async Task Cert_get_pass()
    {
        //Cert must exist in the KeyVault
        var certName = $"existing-cert-name";

        //certificate - the public key and cert metadata.
        var certBytes = await _vault.GetCertAsync(certName);
        Assert.IsNotNull(certBytes);

        //certificate - the private key.
        var certKey = await _vault.GetKeyAsync(certName);
        Assert.IsNotNull(certKey);

        //certificate - export the full X.509 certificate, including its private key (if its policy allows for private key exporting).
        var certSecret = await _vault.GetSecretAsync(certName);
        Assert.IsNotNull(certSecret);

    }
}

using Azure.Security.KeyVault.Keys.Cryptography;
using System.Text;

namespace Package.Infrastructure.KeyVault;
public class KeyVaultCryptoUtility(CryptographyClient cryptoClient) : IKeyVaultCryptoUtility
{
    public async Task<byte[]> EncryptAsync(string plaintext)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintextBytes);
        return encryptResult.Ciphertext;
    }

    public async Task<string> DecryptAsync(byte[] ciphertext)
    {
        var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, ciphertext);
        return Encoding.UTF8.GetString(decryptResult.Plaintext);
    }
}

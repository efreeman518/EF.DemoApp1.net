namespace Package.Infrastructure.KeyVault;

public interface IKeyVaultCryptoUtility
{
    Task<byte[]> EncryptAsync(string plaintext);
    Task<string> DecryptAsync(byte[] ciphertext);
}
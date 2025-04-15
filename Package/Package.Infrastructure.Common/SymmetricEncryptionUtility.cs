using System.Security.Cryptography;

namespace Package.Infrastructure.Common;
public static class SymmetricEncryptionUtility
{
    const CipherMode cipherMode = CipherMode.CBC;
    const PaddingMode paddingMode = PaddingMode.PKCS7;
    const int iVSize = 16; // AES block size in bytes

    public static string Encrypt(string plainText, byte[] key)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Mode = cipherMode;
            aes.Padding = paddingMode;
            aes.Key = key;
            aes.IV = RandomNumberGenerator.GetBytes(iVSize);

            using var memoryStream = new MemoryStream();
            memoryStream.Write(aes.IV, 0, iVSize);

            using (var encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encrypt failed.", ex);
        }
    }

    public static string Decrypt(string cipherText, byte[] key)
    {
        try
        {
            var cipherData = Convert.FromBase64String(cipherText);
            if (cipherData.Length < iVSize)
            {
                throw new ArgumentException("Invalid cipher text format");
            }
            byte[] iv = new byte[iVSize];
            byte[] encryptedData = new byte[cipherData.Length - iVSize];

            Buffer.BlockCopy(cipherData, 0, iv, 0, iVSize);
            Buffer.BlockCopy(cipherData, iVSize, encryptedData, 0, cipherData.Length - iVSize);

            using var aes = Aes.Create();
            aes.Mode = cipherMode;
            aes.Padding = paddingMode;
            aes.Key = key;
            aes.IV = iv;

            using MemoryStream memoryStream = new(encryptedData);
            using var decryptor = aes.CreateDecryptor();
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decrypt failed.", ex);
        }

    }
}

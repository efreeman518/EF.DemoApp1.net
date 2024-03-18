using System.Security.Cryptography;
using System.Text;

namespace Package.Infrastructure.Common;

public static class EncryptionUtility
{
    public static string CreateMD5Hash(string input)
    {
        // Step 1, calculate MD5 hash from input
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);

        // Step 2, convert byte array to hex string
        StringBuilder sb = new();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
    }
}

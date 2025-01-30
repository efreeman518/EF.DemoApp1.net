using System.Security.Cryptography;
using System.Text;

namespace Package.Infrastructure.BlandAI;

public static class Utility
{
    public static bool VerifyWebhookSignature(string key, string data, string signature)
    {
        //using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        //byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        //byte[] hashBytes = hmac.ComputeHash(dataBytes);
        //string expectedSignature = Convert.ToHexStringLower(hashBytes);
        //return expectedSignature == signature;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        //string expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
        string expectedSignature = Convert.ToHexStringLower(hash);

        return expectedSignature == signature;
    }
}

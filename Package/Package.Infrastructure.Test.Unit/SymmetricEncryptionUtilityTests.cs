using Package.Infrastructure.Common;
using System.Security.Cryptography;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class SymmetricEncryptionUtilityTests
{

    [TestMethod]
    public void Encrypt_Decrypt_Success()
    {
        // Arrange
        string plainText = "Hello, World!";
        byte[] key = new byte[32]; // AES-256 key size
        RandomNumberGenerator.Fill(key);
        string encryptedText = SymmetricEncryptionUtility.Encrypt(plainText, key);
        Assert.AreNotEqual(plainText, encryptedText);
        // Act
        string decryptedText = SymmetricEncryptionUtility.Decrypt(encryptedText, key);
        // Assert
        Assert.AreEqual(plainText, decryptedText);
    }

    //write a test for the Decrypt method with invalid key
    [TestMethod]
    public void Decrypt_InvalidKey_ThrowsCryptographicException()
    {
        // Arrange
        string plainText = "Hello, World!";
        byte[] key = new byte[32]; // AES-256 key size
        RandomNumberGenerator.Fill(key);
        string encryptedText = SymmetricEncryptionUtility.Encrypt(plainText, key);
        Assert.AreNotEqual(plainText, encryptedText);
        // Act
        byte[] invalidKey = new byte[32];
        RandomNumberGenerator.Fill(invalidKey);
        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => SymmetricEncryptionUtility.Decrypt(encryptedText, invalidKey));
    }
}

using Azure.Identity;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Package.Infrastructure.Data;

/// <summary>
/// Provide extra functionality for migrations
/// After migration is generated, add appropriate calls inside the migration Up() method
/// VS logged in user must have access to the vault for creating CMK & CEK
/// </summary>
/// Implement AlwaysEncrypted on a column - add something like this at the end of the migration's Up() method:

/* 
    //add to migration class - customize for always encrypted (until supported in fluent syntax)
    string url_AKV_CMK = Environment.GetEnvironmentVariable("AKVCMKURL"); //url to key vault CMK key;
    string schema_table = "[todo].[TodoItem]";
    string cmkName = "CMK_WITH_AKV";

    var support = new MigrationSupport(migrationBuilder, new DefaultAzureCredential());
    support.CreateColumnMasterKey(url_AKV_CMK, cmkName);

    string cekName = "CEK_WITH_AKV";
    support.CreateColumnEncryptionKey(url_AKV_CMK, cmkName, cekName);

    //secure column def; varbinary has no collate
    string colDef = "[SecureDeterministic] varbinary(200)";
    support.AlterColumnEncryption(cekName, schema_table, colDef, encType: "DETERMINISTIC");

    //secure column def; varbinary has no collate
    colDef = "[SecureRandom] varbinary(200)";
    support.AlterColumnEncryption(cekName, schema_table, colDef, encType: "RANDOMIZED");
*/

/*
 * Note also that any identity (application accessing the DB, VS logged in user, etc) using the keys for encryption must have appropriate access to the key vault:
 * https://learn.microsoft.com/en-us/sql/relational-databases/security/encryption/create-and-store-column-master-keys-always-encrypted?view=azuresqldb-current
 * https://www.red-gate.com/simple-talk/databases/sql-server/database-administration-sql-server/sql-server-encryption-always-encrypted/
 * The easiest way to grant the application the required permission is to add its identity to the "Key Vault Crypto User" role
 * 
 * Rotating Keys
 * https://learn.microsoft.com/en-us/sql/relational-databases/security/encryption/rotate-always-encrypted-keys-using-ssms
 */

[ExcludeFromCodeCoverage]
public class MigrationSupport(MigrationBuilder migrationBuilder, DefaultAzureCredential credential)
{
    private readonly SqlColumnEncryptionAzureKeyVaultProvider _akvProvider = new(credential);
    private readonly string s_algorithm = "RSA_OAEP";

    /// <summary>
    /// CMK is based on an AzureKeyVault Key
    /// </summary>
    /// <param name="migrationBuilder"></param>
    /// <param name="akvUrl">url to the azure key vault key used as the column master key </param>
    /// <param name="cmkName"></param>
    /// <param name="sqlColumnEncryptionAzureKeyVaultProvider"></param>
    public void CreateColumnMasterKey(string urlAKVMasterKeyUrl, string cmkName)
    {
        string KeyStoreProviderName = SqlColumnEncryptionAzureKeyVaultProvider.ProviderName;

        byte[] cmkSign = _akvProvider.SignColumnMasterKeyMetadata(urlAKVMasterKeyUrl, true);
        string cmkSignStr = string.Concat("0x", Convert.ToHexString(cmkSign));

        string sql = $@"
IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = '{cmkName}')
BEGIN
CREATE COLUMN MASTER KEY [{cmkName}]
WITH (
    KEY_STORE_PROVIDER_NAME = N'{KeyStoreProviderName}',
    KEY_PATH = N'{urlAKVMasterKeyUrl}',
    ENCLAVE_COMPUTATIONS (SIGNATURE = {cmkSignStr})
);
END
ELSE
BEGIN
    SELECT 'COLUMN MASTER KEY [{cmkName}] exists.'
END
";

        migrationBuilder.Sql(sql);
    }

    /// <summary>
    /// CEK is generated using the CMK
    /// </summary>
    /// <param name="migrationBuilder"></param>
    /// <param name="urlAKVMasterKeyUrl"></param>
    /// <param name="cmkName"></param>
    /// <param name="cekName"></param>
    public void CreateColumnEncryptionKey(string urlAKVMasterKeyUrl, string cmkName, string cekName)
    {
        string sql =
                $@"
IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = '{cekName}')
BEGIN
CREATE COLUMN ENCRYPTION KEY [{cekName}] 
WITH VALUES (
    COLUMN_MASTER_KEY = [{cmkName}],
    ALGORITHM = '{s_algorithm}', 
    ENCRYPTED_VALUE = {GetEncryptedValue(urlAKVMasterKeyUrl)}
);
END
ELSE
BEGIN
    SELECT 'COLUMN ENCRYPTION KEY [{cekName}] exists.';
END";

        migrationBuilder.Sql(sql);
    }

    private string GetEncryptedValue(string urlAKVMasterKeyUrl)
    {
        byte[] plainTextColumnEncryptionKey = new byte[32];
        RandomNumberGenerator.Create().GetBytes(plainTextColumnEncryptionKey);
        byte[] encryptedColumnEncryptionKey = _akvProvider.EncryptColumnEncryptionKey(urlAKVMasterKeyUrl, s_algorithm, plainTextColumnEncryptionKey);
        string EncryptedValue = string.Concat("0x", Convert.ToHexString(encryptedColumnEncryptionKey));
        return EncryptedValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cekName"></param>
    /// <param name="tableName">[schema].[tablename]</param>
    /// <param name="columnNameAndType">[SecureString] NVARCHAR(100)</param>
    /// <param name="collate">If not string then null; COLLATE Latin1_General_BIN2</param>
    /// <param name="encType">DETERMINISTIC or RANDOMIZED; DETERMINISTIC can be used in queries</param>
    /// <param name="algorithm">AEAD_AES_256_CBC_HMAC_SHA_256</param>
    /// <param name="isNull"></param>
    public void AlterColumnEncryption(string cekName, string tableName,
        string columnNameAndType, string? collate = "COLLATE Latin1_General_BIN2", string encType = "DETERMINISTIC",
        string algorithm = "AEAD_AES_256_CBC_HMAC_SHA_256", bool isNull = true)
    {
        string nullability = isNull ? "" : "NOT ";
        string sql = $@"ALTER TABLE {tableName}
                                ALTER COLUMN {columnNameAndType} 
                                {collate} ENCRYPTED WITH(
                                        ENCRYPTION_TYPE = {encType}, 
                                        ALGORITHM = '{algorithm}', 
                                        COLUMN_ENCRYPTION_KEY = [{cekName}]) {nullability}NULL";
        migrationBuilder.Sql(sql);
    }
}

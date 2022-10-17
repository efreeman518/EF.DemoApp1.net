using Azure.Identity;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Security.Cryptography;

namespace Infrastructure.Data;

/// <summary>
/// Provide extra functionality for migrations
/// After migration is generated, add appropriate calls inside the migration Up() method
/// VS logged in user must have access to the vault for creating CMK & CEK
/// </summary>
/// Implement AlwaysEncrypted on a column - add something like this at the end of the migration's Up() method:

#pragma warning disable S1135 // Track uses of "TODO" tags
/* 
    //add to migration class - customize for always encrypted (until supported in fluent syntax)
    string url_AKV_CMK = <url to the keyvault key used as Column Master Key>
    string cmkName = "CMK_WITH_AKV";
    string cekName = "CEK_WITH_AKV";
    string schema_table = "[todo].[TodoItem]";
    string colDef = "[SecretString] nvarchar(100)";
    var support = new MigrationSupport(migrationBuilder, new DefaultAzureCredential());
    support.CreateColumnMasterKey(url_AKV_CMK, cmkName);
    support.CreateColumnEncryptionKey( url_AKV_CMK, cmkName, cekName);
    support.AlterColumnEncryption(cekName, schema_table, colDef);
*/

/*
 * Note also that any identity (application accessing the DB, VS logged in user, etc) using the keys for encryption must have appropriate access to the key vault:
 * https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/create-and-store-column-master-keys-always-encrypted?view=sql-server-ver15
 * The easiest way to grant the application the required permission is to add its identity to the "Key Vault Crypto User" role
 */
#pragma warning restore S1135 // Track uses of "TODO" tags

public class MigrationSupport
{
    private readonly MigrationBuilder _migrationBuilder;
    private readonly SqlColumnEncryptionAzureKeyVaultProvider _akvProvider = null!;
    private readonly string s_algorithm = "RSA_OAEP";

    public MigrationSupport(MigrationBuilder migrationBuilder, DefaultAzureCredential credential)
    {
        _migrationBuilder = migrationBuilder;
        // Initialize AKV provider
        _akvProvider = new(credential);
    }

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
        string cmkSignStr = string.Concat("0x", BitConverter.ToString(cmkSign).Replace("-", string.Empty));

        string sql =
            $@"CREATE COLUMN MASTER KEY [{cmkName}]
                    WITH (
                        KEY_STORE_PROVIDER_NAME = N'{KeyStoreProviderName}',
                        KEY_PATH = N'{urlAKVMasterKeyUrl}',
                        ENCLAVE_COMPUTATIONS (SIGNATURE = {cmkSignStr})
                    );";

        _migrationBuilder.Sql(sql);
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
                $@"CREATE COLUMN ENCRYPTION KEY [{cekName}] 
                        WITH VALUES (
                            COLUMN_MASTER_KEY = [{cmkName}],
                            ALGORITHM = '{s_algorithm}', 
                            ENCRYPTED_VALUE = {GetEncryptedValue(urlAKVMasterKeyUrl)}
                        )";

        _migrationBuilder.Sql(sql);
    }

    private string GetEncryptedValue(string urlAKVMasterKeyUrl)
    {
        byte[] plainTextColumnEncryptionKey = new byte[32];
        RandomNumberGenerator.Create().GetBytes(plainTextColumnEncryptionKey);
        byte[] encryptedColumnEncryptionKey = _akvProvider.EncryptColumnEncryptionKey(urlAKVMasterKeyUrl, s_algorithm, plainTextColumnEncryptionKey);
        string EncryptedValue = string.Concat("0x", BitConverter.ToString(encryptedColumnEncryptionKey).Replace("-", string.Empty));
        return EncryptedValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="migrationBuilder"></param>
    /// <param name="tableName">[schema].[TodoItem]</param>
    /// <param name="columnNameAndType">[SecureString] NVARCHAR(100)</param>
    /// <param name="collate">If not string then null; COLLATE Latin1_General_BIN2</param>
    /// <param name="encType">DETERMINISTIC or RANDOMIZED; DETERMINISTIC can be used in queries</param>
    /// <param name="algorithm">AEAD_AES_256_CBC_HMAC_SHA_256</param>
    /// <param name="cekName"></param>
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
        _migrationBuilder.Sql(sql);
    }
}

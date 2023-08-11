IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'todo') IS NULL EXEC(N'CREATE SCHEMA [todo];');
GO

CREATE TABLE [todo].[TodoItem] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Status] int NOT NULL,
    [SecureRandom] nvarchar(100) NULL,
    [SecureDeterministic] nvarchar(100) NULL,
    [CreatedDate] datetime2(0) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [UpdatedDate] datetime2(0) NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TodoItem] PRIMARY KEY NONCLUSTERED ([Id])
);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_TodoItem_Name] ON [todo].[TodoItem] ([Name]);
GO


IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_WITH_AKV')
BEGIN
CREATE COLUMN MASTER KEY [CMK_WITH_AKV]
WITH (
    KEY_STORE_PROVIDER_NAME = N'AZURE_KEY_VAULT',
    KEY_PATH = N'<keyvault key url>',
    ENCLAVE_COMPUTATIONS (SIGNATURE = <generated from key>)
);
END
ELSE
BEGIN
    SELECT 'COLUMN MASTER KEY [CMK_WITH_AKV] exists.'
END

GO


IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_WITH_AKV')
BEGIN
CREATE COLUMN ENCRYPTION KEY [CEK_WITH_AKV] 
WITH VALUES (
    COLUMN_MASTER_KEY = [CMK_WITH_AKV],
    ALGORITHM = 'RSA_OAEP', 
    ENCRYPTED_VALUE = <generated from key>
);
END
ELSE
BEGIN
    SELECT 'COLUMN ENCRYPTION KEY [CEK_WITH_AKV] exists.';
END
GO

ALTER TABLE [todo].[TodoItem]
                                ALTER COLUMN [SecureDeterministic] nvarchar(100) 
                                COLLATE Latin1_General_BIN2 ENCRYPTED WITH(
                                        ENCRYPTION_TYPE = DETERMINISTIC, 
                                        ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                        COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
GO

ALTER TABLE [todo].[TodoItem]
                                ALTER COLUMN [SecureRandom] nvarchar(100) 
                                COLLATE Latin1_General_BIN2 ENCRYPTED WITH(
                                        ENCRYPTION_TYPE = RANDOMIZED, 
                                        ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                        COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230811150747_InitialCreate', N'7.0.10');
GO

COMMIT;
GO


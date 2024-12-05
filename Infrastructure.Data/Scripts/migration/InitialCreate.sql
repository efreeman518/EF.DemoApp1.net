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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'todo') IS NULL EXEC(N'CREATE SCHEMA [todo];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    CREATE TABLE [todo].[SystemSetting] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(200) NULL,
        [Flags] int NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedDate] datetime2(0) NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedDate] datetime2(0) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SystemSetting] PRIMARY KEY NONCLUSTERED ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    CREATE TABLE [todo].[TodoItem] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Status] int NOT NULL,
        [SecureRandom] varbinary(200) NULL,
        [SecureDeterministic] varbinary(200) NULL,
        [IsDeleted] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TodoItem] PRIMARY KEY NONCLUSTERED ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX [IX_SystemSetting_Key] ON [todo].[SystemSetting] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX [IX_TodoItem_Name] ON [todo].[TodoItem] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN

    IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_WITH_AKV')
    BEGIN
    CREATE COLUMN MASTER KEY [CMK_WITH_AKV]
    WITH (
        KEY_STORE_PROVIDER_NAME = N'AZURE_KEY_VAULT',
        KEY_PATH = N'[Path to SQL-CMK-1]',
        ENCLAVE_COMPUTATIONS (SIGNATURE = 0x0)
    );
    END
    ELSE
    BEGIN
        SELECT 'COLUMN MASTER KEY [CMK_WITH_AKV] exists.'
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN

    IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_WITH_AKV')
    BEGIN
    CREATE COLUMN ENCRYPTION KEY [CEK_WITH_AKV] 
    WITH VALUES (
        COLUMN_MASTER_KEY = [CMK_WITH_AKV],
        ALGORITHM = 'RSA_OAEP', 
        ENCRYPTED_VALUE = 0x0
    );
    END
    ELSE
    BEGIN
        SELECT 'COLUMN ENCRYPTION KEY [CEK_WITH_AKV] exists.';
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    ALTER TABLE [todo].[TodoItem]
                                    ALTER COLUMN [SecureDeterministic] varbinary(200) 
                                     ENCRYPTED WITH(
                                            ENCRYPTION_TYPE = DETERMINISTIC, 
                                            ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                            COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    ALTER TABLE [todo].[TodoItem]
                                    ALTER COLUMN [SecureRandom] varbinary(200) 
                                     ENCRYPTED WITH(
                                            ENCRYPTION_TYPE = RANDOMIZED, 
                                            ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256', 
                                            COLUMN_ENCRYPTION_KEY = [CEK_WITH_AKV]) NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20241202164906_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20241202164906_InitialCreate', N'9.0.0');
END;

COMMIT;
GO


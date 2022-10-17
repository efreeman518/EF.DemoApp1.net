Begin Try

BEGIN TRANSACTION;

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF SCHEMA_ID(N'todo') IS NULL EXEC(N'CREATE SCHEMA [todo];');

CREATE TABLE [todo].[TodoItem] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [IsComplete] bit NOT NULL,
    [Status] int NOT NULL,
    [CreatedDate] datetime2(0) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [UpdatedDate] datetime2(0) NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_TodoItem] PRIMARY KEY NONCLUSTERED ([Id])
);

CREATE UNIQUE CLUSTERED INDEX [IX_TodoItem_Name] ON [todo].[TodoItem] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20210623215531_InitialCreate', N'5.0.7');

COMMIT;

End Try
Begin Catch
	Select 
		ERROR_NUMBER() AS ErrorNumber
		,ERROR_SEVERITY() as ErrorSeverity
		,ERROR_STATE() as ErrorState
		,ERROR_PROCEDURE() as ErrorProcedure
		,ERROR_LINE() as ErrorLine
		,ERROR_MESSAGE() as ErrorMessage;
	Rollback Transaction;
End Catch;




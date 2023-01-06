Begin Try

BEGIN TRANSACTION;

DROP TABLE [todo].[TodoItem];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20210623215531_InitialCreate';

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



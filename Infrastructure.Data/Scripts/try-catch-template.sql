Begin Try
Begin Transaction;

--script here

Commit Transaction;

End Try
Begin Catch
	Select 
		ERROR_NUMBER() AS ErrorNumber
        , ERROR_SEVERITY() as ErrorSeverity
		, ERROR_STATE() as ErrorState
		, ERROR_PROCEDURE() as ErrorProcedure
		, ERROR_LINE() as ErrorLine
		, ERROR_MESSAGE() as ErrorMessage;
Rollback Transaction;
End Catch;
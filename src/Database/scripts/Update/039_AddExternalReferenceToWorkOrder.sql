BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding ExternalReference column to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder] ADD [ExternalReference] NVARCHAR(100) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

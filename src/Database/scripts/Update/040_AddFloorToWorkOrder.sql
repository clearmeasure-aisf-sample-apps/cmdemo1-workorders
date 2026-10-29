BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding Floor column to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder] ADD [Floor] nvarchar(20) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

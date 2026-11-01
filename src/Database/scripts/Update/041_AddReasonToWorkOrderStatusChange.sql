BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding Reason column to [dbo].[WorkOrderStatusChange]'
	ALTER TABLE [dbo].[WorkOrderStatusChange] ADD [Reason] NVARCHAR(200) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

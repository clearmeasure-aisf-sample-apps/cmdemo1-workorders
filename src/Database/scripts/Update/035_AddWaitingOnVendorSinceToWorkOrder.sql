BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding WaitingOnVendorSince column to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder] ADD [WaitingOnVendorSince] DATETIME2 NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

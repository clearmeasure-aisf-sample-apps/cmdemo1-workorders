BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding Priority and Building columns to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder] ADD
		[Priority] tinyint NOT NULL CONSTRAINT [DF_WorkOrder_Priority] DEFAULT 2,
		[Building] nvarchar(100) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

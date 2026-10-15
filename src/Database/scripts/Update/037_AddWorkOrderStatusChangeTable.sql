BEGIN TRY
	BEGIN TRANSACTION

	PRINT N'Creating table [dbo].[WorkOrderStatusChange]'
	CREATE TABLE [dbo].[WorkOrderStatusChange]
	(
		[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		[WorkOrderId] UNIQUEIDENTIFIER NOT NULL,
		[FromStatus] NVARCHAR(3) NOT NULL,
		[ToStatus] NVARCHAR(3) NOT NULL,
		[ChangedById] UNIQUEIDENTIFIER NOT NULL,
		[ChangedDate] DATETIME2 NOT NULL,
		CONSTRAINT [FK_WorkOrderStatusChange_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder]([Id]) ON DELETE CASCADE,
		CONSTRAINT [FK_WorkOrderStatusChange_Employee] FOREIGN KEY ([ChangedById]) REFERENCES [dbo].[Employee]([Id])
	)

	PRINT N'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

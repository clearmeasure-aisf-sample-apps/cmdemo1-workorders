BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Creating table [dbo].[SavedSearchView]'
	CREATE TABLE [dbo].[SavedSearchView] (
		[Id] UNIQUEIDENTIFIER NOT NULL,
		[EmployeeId] UNIQUEIDENTIFIER NOT NULL,
		[Name] NVARCHAR(100) NOT NULL,
		[Query] NVARCHAR(1000) NOT NULL,
		[CreatedDate] DATETIME2 NOT NULL,
		CONSTRAINT [PK_SavedSearchView] PRIMARY KEY CLUSTERED ([Id]),
		CONSTRAINT [FK_SavedSearchView_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE CASCADE
	)

	PRINT N'Creating unique index [IX_SavedSearchView_EmployeeId_Name] on [dbo].[SavedSearchView]'
	CREATE UNIQUE NONCLUSTERED INDEX [IX_SavedSearchView_EmployeeId_Name] ON [dbo].[SavedSearchView] ([EmployeeId], [Name])
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

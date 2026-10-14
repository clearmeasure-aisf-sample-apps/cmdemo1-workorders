BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Dropping index [IX_WorkOrder_Number] (declared on [Id])'
	DROP INDEX IF EXISTS [IX_WorkOrder_Number] ON [dbo].[WorkOrder]

	PRINT N'Renumbering later duplicates of [dbo].[WorkOrder].[Number]'
	DECLARE @Duplicates TABLE ([Id] uniqueidentifier NOT NULL PRIMARY KEY)
	INSERT INTO @Duplicates ([Id])
	SELECT [Id]
	FROM (
		SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [Number] ORDER BY [CreatedDate], [Id]) AS [Occurrence]
		FROM [dbo].[WorkOrder]
	) AS [Numbered]
	WHERE [Occurrence] > 1

	DECLARE @Id uniqueidentifier
	DECLARE @NewNumber nvarchar(7)
	WHILE EXISTS (SELECT 1 FROM @Duplicates)
	BEGIN
		SELECT TOP (1) @Id = [Id] FROM @Duplicates
		SET @NewNumber = UPPER(LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), 7))
		WHILE EXISTS (SELECT 1 FROM [dbo].[WorkOrder] WHERE [Number] = @NewNumber)
		BEGIN
			SET @NewNumber = UPPER(LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), 7))
		END
		UPDATE [dbo].[WorkOrder] SET [Number] = @NewNumber WHERE [Id] = @Id
		DELETE FROM @Duplicates WHERE [Id] = @Id
	END

	PRINT N'Creating unique index [IX_WorkOrder_Number] on [dbo].[WorkOrder] ([Number])'
	CREATE UNIQUE NONCLUSTERED INDEX [IX_WorkOrder_Number] ON [dbo].[WorkOrder] ([Number])
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO

BEGIN TRY
	BEGIN TRANSACTION
	-- The old IX_WorkOrder_Number is declared on [Id] and enforces foreign keys that reference
	-- WorkOrder(Id), so it cannot be dropped: rename it and give the name to the [Number] index.
	IF EXISTS (
		SELECT 1
		FROM sys.indexes i
		INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
		INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
		WHERE i.object_id = OBJECT_ID(N'[dbo].[WorkOrder]') AND i.name = N'IX_WorkOrder_Number' AND c.name = N'Id')
	BEGIN
		PRINT N'Renaming index [IX_WorkOrder_Number] (declared on [Id]) to [IX_WorkOrder_Id]'
		EXEC sp_rename N'dbo.WorkOrder.IX_WorkOrder_Number', N'IX_WorkOrder_Id', N'INDEX';
	END

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
		ROLLBACK TRANSACTION;
	THROW;
END CATCH
GO

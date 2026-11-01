BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Creating [dbo].[Team]'
	CREATE TABLE [dbo].[Team]
	(
		[Id]			UNIQUEIDENTIFIER	NOT NULL,
		[Name]			NVARCHAR(100)		NOT NULL,
		[Description]	NVARCHAR(500)		NULL,
		CONSTRAINT [PK_Team] PRIMARY KEY CLUSTERED ([Id])
	)
	CREATE UNIQUE NONCLUSTERED INDEX [UX_Team_Name] ON [dbo].[Team] ([Name])

	PRINT N'Creating [dbo].[Vendor]'
	CREATE TABLE [dbo].[Vendor]
	(
		[Id]			UNIQUEIDENTIFIER	NOT NULL,
		[Name]			NVARCHAR(100)		NOT NULL,
		[Trade]			NVARCHAR(100)		NULL,
		[ContactName]	NVARCHAR(100)		NULL,
		[EmailAddress]	NVARCHAR(255)		NULL,
		[PhoneNumber]	NVARCHAR(50)		NULL,
		[IsActive]		BIT					NOT NULL CONSTRAINT [DF_Vendor_IsActive] DEFAULT 1,
		CONSTRAINT [PK_Vendor] PRIMARY KEY CLUSTERED ([Id])
	)
	CREATE UNIQUE NONCLUSTERED INDEX [UX_Vendor_Name] ON [dbo].[Vendor] ([Name])

	PRINT N'Creating [dbo].[TeamMember]'
	CREATE TABLE [dbo].[TeamMember]
	(
		[TeamId]		UNIQUEIDENTIFIER	NOT NULL,
		[EmployeeId]	UNIQUEIDENTIFIER	NOT NULL,
		CONSTRAINT [PK_TeamMember] PRIMARY KEY CLUSTERED ([TeamId], [EmployeeId]),
		CONSTRAINT [FK_TeamMember_Team] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Team] ([Id]) ON DELETE CASCADE,
		CONSTRAINT [FK_TeamMember_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id])
	)
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO

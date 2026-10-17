using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess;

/// <summary>
/// SQLite's EnsureCreated is a no-op when the database file already has any tables, so tables added after a
/// file was first created are never created. This applies idempotent creation of such tables (SQLite only;
/// SQL Server schema is managed by the DbUp scripts).
/// </summary>
public static class SqliteSchemaUpgrade
{
    public const string CreateSavedSearchViewSql = """
        CREATE TABLE IF NOT EXISTS "SavedSearchView" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_SavedSearchView" PRIMARY KEY,
            "EmployeeId" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Query" TEXT NOT NULL,
            "CreatedDate" TEXT NOT NULL,
            CONSTRAINT "FK_SavedSearchView_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_SavedSearchView_EmployeeId_Name" ON "SavedSearchView" ("EmployeeId", "Name");
        """;

    public const string CreateTeamAndVendorSql = """
        CREATE TABLE IF NOT EXISTS "Team" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Team" PRIMARY KEY,
            "Name" TEXT NOT NULL,
            "Description" TEXT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Team_Name" ON "Team" ("Name");
        CREATE TABLE IF NOT EXISTS "Vendor" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Vendor" PRIMARY KEY,
            "Name" TEXT NOT NULL,
            "Trade" TEXT NULL,
            "ContactName" TEXT NULL,
            "EmailAddress" TEXT NULL,
            "PhoneNumber" TEXT NULL,
            "IsActive" INTEGER NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Vendor_Name" ON "Vendor" ("Name");
        CREATE TABLE IF NOT EXISTS "TeamMember" (
            "TeamId" TEXT NOT NULL,
            "EmployeeId" TEXT NOT NULL,
            CONSTRAINT "PK_TeamMember" PRIMARY KEY ("TeamId", "EmployeeId"),
            CONSTRAINT "FK_TeamMember_Team_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Team" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_TeamMember_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_TeamMember_EmployeeId" ON "TeamMember" ("EmployeeId");
        """;

    /// <summary>EnsureCreated, then idempotently add tables that may be missing from a pre-existing SQLite file.</summary>
    public static void EnsureCreatedAndUpgraded(DbContext context)
    {
        context.Database.EnsureCreated();
        if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Database.ExecuteSqlRaw(CreateSavedSearchViewSql);
            context.Database.ExecuteSqlRaw(CreateTeamAndVendorSql);
        }
    }
}

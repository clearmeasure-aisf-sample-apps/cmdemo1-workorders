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

    /// <summary>EnsureCreated, then idempotently add tables that may be missing from a pre-existing SQLite file.</summary>
    public static void EnsureCreatedAndUpgraded(DbContext context)
    {
        context.Database.EnsureCreated();
        if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Database.ExecuteSqlRaw(CreateSavedSearchViewSql);
        }
    }
}

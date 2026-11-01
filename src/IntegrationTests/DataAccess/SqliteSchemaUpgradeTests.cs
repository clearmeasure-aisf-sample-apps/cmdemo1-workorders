using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class SqliteSchemaUpgradeTests
{
    [Test]
    public void ShouldCreateSavedSearchViewTable_WhenMissingFromExistingDatabase_AndBeIdempotent()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        void Exec(string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        Exec("""CREATE TABLE "Employee" ("Id" TEXT NOT NULL PRIMARY KEY)""");

        Exec(SqliteSchemaUpgrade.CreateSavedSearchViewSql);
        Exec(SqliteSchemaUpgrade.CreateSavedSearchViewSql);

        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('SavedSearchView','IX_SavedSearchView_EmployeeId_Name')";
        Convert.ToInt32(check.ExecuteScalar()).ShouldBe(2);
    }

    [Test]
    public void ShouldCreateTeamAndVendorTables_WhenMissingFromExistingDatabase_AndBeIdempotent()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        void Exec(string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        Exec("""CREATE TABLE "Employee" ("Id" TEXT NOT NULL PRIMARY KEY)""");
        Exec(SqliteSchemaUpgrade.CreateTeamAndVendorSql);
        Exec(SqliteSchemaUpgrade.CreateTeamAndVendorSql);

        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('Team','Vendor','TeamMember')";
        Convert.ToInt32(check.ExecuteScalar()).ShouldBe(3);
    }
}

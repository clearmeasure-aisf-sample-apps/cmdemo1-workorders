using System.Globalization;
using ClearMeasure.Bootcamp.Core;
using DbUp;
using DbUp.Engine.Output;
using DbUp.ScriptProviders;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>
/// Applies <c>036_AddTeamAndVendorTables.sql</c> with DbUp to a scratch SQL Server database that is at the
/// previous script, and checks that the existing schema and data are unchanged and the new tables exist.
/// </summary>
[TestFixture]
public class TeamVendorMigrationTests
{
    private const string ScriptName = "036_AddTeamAndVendorTables.sql";
    private static readonly Guid SeededEmployeeId = new("6f1c8d2e-0b7a-4d4e-9a35-2b1d7c0e2911");
    private static readonly string[] NewTables = ["Team", "Vendor", "TeamMember"];

    private string? _connectionString;

    [TearDown]
    public void TearDown()
    {
        if (_connectionString == null)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        DropDatabase.For.SqlDatabase(_connectionString, new NoOpUpgradeLog());
        _connectionString = null;
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldApply036OnDatabaseAtPreviousScriptAndLeaveExistingTablesUnchanged()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        var builder = new SqlConnectionStringBuilder(
            TestHost.GetRequiredService<IDatabaseConfiguration>().GetConnectionString())
        {
            InitialCatalog = "ChurchBulletin_Mig291_" + Guid.NewGuid().ToString("N")[..8]
        };
        _connectionString = builder.ConnectionString;
        EnsureDatabase.For.SqlDatabase(_connectionString, new NoOpUpgradeLog());
        var scriptDirectory = Path.Combine(FindRepoRoot(), "src", "Database", "scripts", "Update");

        var previous = Upgrade(scriptDirectory,
            file => string.CompareOrdinal(Path.GetFileName(file), "036_") < 0);
        previous.Successful.ShouldBeTrue(previous.Error?.ToString());
        TableExists("Employee").ShouldBeTrue();
        NewTables.Select(TableExists).ShouldAllBe(exists => !exists);
        var schemaBefore = SnapshotExistingSchema();
        Execute("INSERT INTO [dbo].[Employee] ([Id], [UserName], [FirstName], [LastName], [EmailAddress], " +
                "[PreferredLanguage]) VALUES (@id, N'mig291', N'Migration', N'Check', N'mig291@example.com', N'fr-FR')",
            new SqlParameter("@id", SeededEmployeeId));

        var full = Upgrade(scriptDirectory, null);

        full.Successful.ShouldBeTrue(full.Error?.ToString());
        full.Scripts.Select(s => Path.GetFileName(s.Name)).Where(n => string.CompareOrdinal(n, "037_") < 0).ShouldBe([ScriptName]);
        SnapshotExistingSchema().ShouldBe(schemaBefore);
        Query("SELECT [UserName], [FirstName], [LastName], [EmailAddress], [PreferredLanguage] " +
              "FROM [dbo].[Employee] WHERE [Id] = @id",
                new SqlParameter("@id", SeededEmployeeId))
            .ShouldBe(["mig291|Migration|Check|mig291@example.com|fr-FR"]);
        NewTables.Select(TableExists).ShouldAllBe(exists => exists);
        ColumnsOf("Team").ShouldBe(["Id|uniqueidentifier||NO", "Name|nvarchar|100|NO", "Description|nvarchar|500|YES"]);
        ColumnsOf("Vendor").ShouldBe([
            "Id|uniqueidentifier||NO", "Name|nvarchar|100|NO", "Trade|nvarchar|100|YES",
            "ContactName|nvarchar|100|YES", "EmailAddress|nvarchar|255|YES", "PhoneNumber|nvarchar|50|YES",
            "IsActive|bit||NO", "Notes|nvarchar|1000|YES"
        ]);
        ColumnsOf("TeamMember").ShouldBe(["TeamId|uniqueidentifier||NO", "EmployeeId|uniqueidentifier||NO"]);
        Execute("INSERT INTO [dbo].[Vendor] ([Id], [Name]) VALUES (NEWID(), N'Acme')");
        Query("SELECT [IsActive] FROM [dbo].[Vendor] WHERE [Name] = N'Acme'").ShouldBe(["True"]);
    }

    private DbUp.Engine.DatabaseUpgradeResult Upgrade(string scriptDirectory, Func<string, bool>? filter)
    {
        var options = new FileSystemScriptOptions();
        if (filter != null)
        {
            options.Filter = filter;
        }

        return DeployChanges.To
            .SqlDatabase(_connectionString)
            .WithScriptsFromFileSystem(scriptDirectory, options)
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogToNowhere()
            .Build()
            .PerformUpgrade();
    }

    private string[] SnapshotExistingSchema()
    {
        const string excluded = "('SchemaVersions', 'Team', 'Vendor', 'TeamMember', 'WorkOrderStatusChange')";
        var columns = Query(
            "SELECT 'C', TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, DATA_TYPE, " +
            "CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT " +
            "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME NOT IN " + excluded +
            " AND NOT (TABLE_NAME = 'WorkOrder' AND COLUMN_NAME = 'ExternalReference')" +
            " AND NOT (TABLE_NAME = 'WorkOrder' AND COLUMN_NAME = 'Floor')");
        var constraints = Query(
            "SELECT 'K', TABLE_SCHEMA, TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE " +
            "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME NOT IN " + excluded);
        var indexes = Query(
            "SELECT 'I', SCHEMA_NAME(t.schema_id), t.name, i.name, i.type_desc, i.is_unique " +
            "FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id " +
            "WHERE t.name NOT IN " + excluded);

        return columns.Concat(constraints).Concat(indexes).OrderBy(row => row, StringComparer.Ordinal).ToArray();
    }

    private string[] ColumnsOf(string table) =>
        Query("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE " +
              "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table " +
              "ORDER BY ORDINAL_POSITION", new SqlParameter("@table", table));

    private bool TableExists(string table) =>
        Query("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table",
            new SqlParameter("@table", table)).Length == 1;

    private void Execute(string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }

    // Columns are joined on the client: concatenating in SQL mixes the catalog collation of sys.* names
    // with the database collation and fails with a collation conflict.
    private string[] Query(string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = reader.IsDBNull(i)
                    ? ""
                    : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? "";
            }

            rows.Add(string.Join("|", values));
        }

        return rows.ToArray();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}

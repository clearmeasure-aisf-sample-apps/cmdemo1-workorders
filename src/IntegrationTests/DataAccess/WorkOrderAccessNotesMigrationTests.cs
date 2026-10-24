using System.Globalization;
using ClearMeasure.Bootcamp.Core;
using DbUp;
using DbUp.Engine.Output;
using DbUp.ScriptProviders;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderAccessNotesMigrationTests
{
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
    public void ShouldApply040WithNullableAccessNotesColumn()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        var builder = new SqlConnectionStringBuilder(
            TestHost.GetRequiredService<IDatabaseConfiguration>().GetConnectionString())
        {
            InitialCatalog = "ChurchBulletin_AccNotes_" + Guid.NewGuid().ToString("N")[..8]
        };
        _connectionString = builder.ConnectionString;
        EnsureDatabase.For.SqlDatabase(_connectionString, new NoOpUpgradeLog());
        var scriptDirectory = Path.Combine(FindRepoRoot(), "src", "Database", "scripts", "Update");

        var previous = Upgrade(scriptDirectory, file => string.CompareOrdinal(Path.GetFileName(file), "040_") < 0);
        previous.Successful.ShouldBeTrue(previous.Error?.ToString());
        ColumnsOf("AccessNotes").ShouldBeEmpty();

        var full = Upgrade(scriptDirectory, null);

        full.Successful.ShouldBeTrue(full.Error?.ToString());
        full.Scripts.Select(s => Path.GetFileName(s.Name)).ShouldContain("040_AddAccessNotesToWorkOrder.sql");
        ColumnsOf("AccessNotes").ShouldBe(["nvarchar|500|YES"]);
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

    private string[] ColumnsOf(string column) =>
        Query("SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE " +
              "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'WorkOrder' " +
              "AND COLUMN_NAME = @column",
            new SqlParameter("@column", column));

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

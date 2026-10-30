using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class DatabaseMigrationVersionReaderTests
{
    private string _dbPath = null!;
    private DataContext _context = null!;
    private DatabaseMigrationVersionReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dbver_{Guid.NewGuid():N}.db");
        _context = new DataContext(new FileDbConfig($"Data Source={_dbPath}"));
        _context.Database.EnsureCreated();
        _reader = new DatabaseMigrationVersionReader(_context, NullLogger<DatabaseMigrationVersionReader>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task Should_ReturnHighestScriptNumber_When_JournalHasPathsDuplicatesAndNonNumericNames()
    {
        await CreateJournalAsync();
        await InsertAsync(@"C:\scripts\001-first-script.sql");
        await InsertAsync("/tmp/007_Early.sql");
        await InsertAsync("/var/scripts/040_AddFloorToWorkOrder.sql");
        await InsertAsync(@"D:\040_AddAccessNotesToWorkOrder.sql");
        await InsertAsync("notes.txt");

        var version = await _reader.ReadAsync();

        version.ShouldBe(40);
    }

    [Test]
    public async Task Should_ReturnSeven_When_OnlyZeroPaddedScriptIsApplied()
    {
        await CreateJournalAsync();
        await InsertAsync("007_Early.sql");

        var version = await _reader.ReadAsync();

        version.ShouldBe(7);
    }

    [Test]
    public async Task Should_ReturnNull_When_JournalIsEmpty()
    {
        await CreateJournalAsync();

        var version = await _reader.ReadAsync();

        version.ShouldBeNull();
    }

    [Test]
    public async Task Should_ReturnNull_When_JournalTableIsMissing()
    {
        var version = await _reader.ReadAsync();

        version.ShouldBeNull();
    }

    private async Task CreateJournalAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE SchemaVersions (
                Id INTEGER PRIMARY KEY,
                ScriptName TEXT NOT NULL,
                Applied TEXT NOT NULL
            );
            """);
    }

    private async Task InsertAsync(string scriptName)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            scriptName,
            "2020-01-01");
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}

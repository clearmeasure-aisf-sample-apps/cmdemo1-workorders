using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HealthCheckDatabaseVersionIntegrationTests
{
    private string _dbPath = null!;
    private FileSqliteHealthFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"hcver_{Guid.NewGuid():N}.db");
        _factory = new FileSqliteHealthFactory($"Data Source={_dbPath}");
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS SchemaVersions (
                Id INTEGER PRIMARY KEY,
                ScriptName TEXT NOT NULL,
                Applied TEXT NOT NULL
            );
            """);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            "001-first-script.sql",
            "2020-01-01");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            "/tmp/039_AddExternalReferenceToWorkOrder.sql",
            "2020-01-02");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            @"C:\scripts\040_AddFloorToWorkOrder.sql",
            "2020-01-03");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            "040_AddAccessNotesToWorkOrder.sql",
            "2020-01-04");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES ({0}, {1})",
            "readme.txt",
            "2020-01-05");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task Should_IncludeHighestScriptNumber_When_JournalHasAppliedScripts()
    {
        var response = await _client!.GetAsync("/_healthcheck");
        var body = await response.Content.ReadAsStringAsync();
        var lines = body.Split('\n');

        lines.Length.ShouldBe(2);
        lines[0].ShouldBeOneOf("Healthy", "Degraded", "Unhealthy");
        lines[1].ShouldBe("databaseVersion: 40");
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        response.StatusCode.ShouldBe(lines[0] is "Healthy" or "Degraded"
            ? HttpStatusCode.OK
            : HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task Should_LeaveDetailedHealthShapeUnchanged_When_DatabaseVersionIsReported()
    {
        var response = await _client!.GetAsync("/_healthcheck/detailed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("overallStatus", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("entries", out var entries).ShouldBeTrue();
        entries.ValueKind.ShouldBe(JsonValueKind.Array);
        doc.RootElement.TryGetProperty("databaseVersion", out _).ShouldBeFalse();
    }

    private sealed class FileSqliteHealthFactory(string connectionString)
        : WebApplicationFactory<UiServerWebApplicationMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SqlConnectionString", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnectionString"] = connectionString,
                    ["AI_OpenAI_ApiKey"] = "",
                    ["AI_OpenAI_Url"] = "",
                    ["AI_OpenAI_Model"] = "",
                    ["ApiKeyAuthentication:Enabled"] = "true",
                    ["ApiKeyAuthentication:ValidationKey"] = "integration-test-api-key",
                    ["ApiRateLimiting:Enabled"] = "true",
                    ["ApiRateLimiting:PermitLimit"] = "100",
                    ["ApiRateLimiting:WindowSeconds"] = "60",
                    ["ApiRateLimiting:SegmentsPerWindow"] = "4",
                    ["ApiRateLimiting:QueueLimit"] = "0"
                });
            });
        }
    }
}

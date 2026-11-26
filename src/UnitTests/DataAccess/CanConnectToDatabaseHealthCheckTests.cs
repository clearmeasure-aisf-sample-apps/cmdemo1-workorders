using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class CanConnectToDatabaseHealthCheckTests
{
    [Test]
    public async Task Should_ReportHealthy_When_DatabaseAcceptsConnection()
    {
        await using var dbContext = CreateContext("Data Source=:memory:");

        var result = await CreateCheck(dbContext).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task Should_ReportUnhealthy_When_DatabaseFileDoesNotExist()
    {
        await using var dbContext = CreateContext($"Data Source={Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.db")}");

        var result = await CreateCheck(dbContext).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeNull();
    }

    [Test]
    public async Task Should_ReportUnhealthyWithException_When_CheckIsCancelled()
    {
        await using var dbContext = CreateContext("Data Source=:memory:");

        var result = await CreateCheck(dbContext).CheckHealthAsync(new HealthCheckContext(), new CancellationToken(true));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    private static DbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder().UseSqlite(connectionString).Options);

    private static CanConnectToDatabaseHealthCheck CreateCheck(DbContext dbContext) =>
        new(dbContext, NullLogger<CanConnectToDatabaseHealthCheck>.Instance);
}

using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HealthCheckPlainTextResponseWriterTests
{
    [Test]
    public void Should_KeepStatusWordAndAppendDatabaseVersion_When_FormattingHealthBody()
    {
        var body = HealthCheckPlainTextResponseWriter.Format(HealthStatus.Healthy, 40);

        body.ShouldBe("Healthy\ndatabaseVersion: 40");
    }

    [Test]
    public void Should_WriteNullDatabaseVersion_When_VersionIsMissing()
    {
        var body = HealthCheckPlainTextResponseWriter.Format(HealthStatus.Unhealthy, null);

        body.ShouldBe("Unhealthy\ndatabaseVersion: null");
    }

    [Test]
    public async Task Should_WritePlainTextNullVersion_When_ReaderIsNotRegistered()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["API"] = new(HealthStatus.Degraded, "degraded", TimeSpan.Zero, null, null)
            },
            TimeSpan.Zero);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await HealthCheckPlainTextResponseWriter.WriteAsync(context, report);

        context.Response.ContentType.ShouldBe("text/plain; charset=utf-8");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.ShouldBe("Degraded\ndatabaseVersion: null");
    }
}

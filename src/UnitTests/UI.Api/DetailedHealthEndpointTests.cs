using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DetailedHealthEndpointTests
{
    private sealed class FixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class StubDetailedHealthReportProvider(DetailedHealthReport report) : IDetailedHealthReportProvider
    {
        public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }

    private static DetailedHealthReport FixedReport(DateTime checkedAtUtc) => new()
    {
        OverallStatus = ComponentHealthStatus.Healthy,
        CheckedAtUtc = checkedAtUtc,
        ProcessId = 42,
        OsDescription = "Test OS",
        FrameworkDescription = "Test Framework",
        GcMemoryMb = 100,
        WorkingSetMb = 200,
        ProcessorCount = 4,
        Is64BitProcess = true,
        TimeZoneId = "UTC",
        ProcessPriority = "Normal",
        Components =
        [
            new ComponentHealthEntry { Name = "API", Status = ComponentHealthStatus.Healthy }
        ]
    };

    [Test]
    public async Task DetailedHealthController_Get_Should_ReturnOkReport()
    {
        var fixedTime = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
        var endpoint = CreateEndpoint(fixedTime);

        var result = await endpoint.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeContentResult();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("application/json");
        content.Content.ShouldNotBeNull();
        var payload = JsonSerializer.Deserialize<DetailedHealthReport>(
            content.Content,
            ConditionalGetEtag.JsonSerializerOptions);
        payload = payload.ShouldNotBeNull();
        payload.OverallStatus.ShouldBe(ComponentHealthStatus.Healthy);
        payload.Components.Count.ShouldBe(1);
        payload.Components[0].Name.ShouldBe("API");
        payload.Components[0].Status.ShouldBe(ComponentHealthStatus.Healthy);
    }

    [Test]
    public async Task GetDetailed_Should_SetEtagHeader_When_ReportReturned()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var endpoint = CreateEndpoint(fixedTime);

        var result = await endpoint.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeContentResult();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        endpoint.Response.Headers.ETag.ToString().ShouldStartWith("W/\"");
    }

    [Test]
    public async Task GetDetailed_Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var report = FixedReport(fixedTime);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(DetailedHealthEtagFingerprint.FromReport(report));

        var endpoint = CreateEndpoint(fixedTime);
        endpoint.Request.Headers.IfNoneMatch = etag.ToString();

        var result = await endpoint.GetDetailed(CancellationToken.None);

        result.ShouldBeStatusCodeResult().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public async Task GetDetailed_Should_Return200_When_IfNoneMatchDiffers()
    {
        var fixedTime = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var endpoint = CreateEndpoint(fixedTime);
        endpoint.Request.Headers.IfNoneMatch = "W/\"stale-etag\"";

        var result = await endpoint.GetDetailed(CancellationToken.None);

        var content = result.ShouldBeContentResult();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.Content.ShouldNotBeNullOrEmpty();
    }

    private static DetailedHealthUnderTest CreateEndpoint(DateTime fixedTime) =>
        new(new FixedUtcTimeProvider(fixedTime), new StubDetailedHealthReportProvider(FixedReport(fixedTime)));

    private sealed class DetailedHealthUnderTest(TimeProvider timeProvider, IDetailedHealthReportProvider reportProvider)
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();
        public HttpRequest Request => HttpContext.Request;
        public HttpResponse Response => HttpContext.Response;
        public IResult Get() => DetailedHealthEndpoint.Get(HttpContext, timeProvider);
        public Task<IResult> GetDetailed(CancellationToken cancellationToken) =>
            DetailedHealthEndpoint.GetDetailed(HttpContext, reportProvider, cancellationToken);
    }
}

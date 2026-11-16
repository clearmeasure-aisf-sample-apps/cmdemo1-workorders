using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary>
/// Unit coverage for <see cref="MetricsEndpoint"/> (issue #9158 metrics summary contract).
/// </summary>
[TestFixture]
public class MetricsEndpointTests
{
    private sealed class StubRequestCounter(long total) : IHttpRequestMetricsCounter
    {
        public long Total => total;
        public void Increment() { }
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void GetSummary_Should_ReturnJson_WithRequiredFields_When_Called()
    {
        var endpoint = CreateEndpoint(totalRequests: 7);

        var result = endpoint.GetSummary();

        var content = result.ShouldBeContentResult();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<MetricsSummaryResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.TotalRequestsServed.ShouldBe(7);
        payload.WorkingSetBytes.ShouldBeGreaterThan(0);
        payload.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void GetSummary_Should_SetWeakEtag_When_Called()
    {
        var endpoint = CreateEndpoint(totalRequests: 3);

        endpoint.GetSummary();

        var etag = endpoint.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();
        etag.ShouldStartWith("W/");
    }

    [Test]
    public void GetSummary_Should_Return304_When_IfNoneMatchIsAny()
    {
        var endpoint = CreateEndpoint(totalRequests: 3);
        endpoint.Request.Headers.IfNoneMatch = "*";

        var result = endpoint.GetSummary();

        var status = result.ShouldBeStatusCodeResult();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
        endpoint.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    private static MetricsUnderTest CreateEndpoint(long totalRequests)
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        return new MetricsUnderTest(clock, new StubRequestCounter(totalRequests));
    }

    private sealed class MetricsUnderTest(TimeProvider timeProvider, IHttpRequestMetricsCounter requestCounter)
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();
        public HttpRequest Request => HttpContext.Request;
        public HttpResponse Response => HttpContext.Response;
        public IResult GetSummary() => MetricsEndpoint.GetSummary(HttpContext, timeProvider, requestCounter);
    }
}

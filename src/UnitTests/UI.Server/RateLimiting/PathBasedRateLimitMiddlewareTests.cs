using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class PathBasedRateLimitMiddlewareTests
{
    [TestCase("/api/blazor-wasm-single-api")]
    [TestCase("/api/v1.0/blazor-wasm-single-api")]
    [TestCase("/mcp")]
    public async Task InvokeAsync_Answers429_WhenPathGateMatchesWithoutAttributeAndPermitRefused(string path)
    {
        var context = new DefaultHttpContext { Request = { Path = path }, Response = { Body = new MemoryStream() } };

        var called = await InvokeAsync(context, RefusingLimiter());

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.ContainsKey(RateLimitingMiddleware.HeaderLimit).ShouldBeTrue();
    }

    [Test]
    public async Task InvokeAsync_AddsRateLimitHeaders_WhenSingleApiPermitGranted()
    {
        var context = new DefaultHttpContext { Request = { Path = "/api/blazor-wasm-single-api" } };
        var allowing = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetNoLimiter(string.Empty));

        var called = await InvokeAsync(context, allowing);

        called.ShouldBeTrue();
        context.Response.Headers.ContainsKey(RateLimitingMiddleware.HeaderLimit).ShouldBeTrue();
        context.Response.Headers.ContainsKey(RateLimitingMiddleware.HeaderRemaining).ShouldBeTrue();
    }

    [Test]
    public void CreateLimiter_PartitionsMcpPath_WhenLimitingEnabled()
    {
        var monitor = new StubOptionsMonitor<ApiRateLimitingOptions>(
            new ApiRateLimitingOptions { Enabled = true, PermitLimit = 1, WindowSeconds = 60, QueueLimit = 0 });
        using var limiter = ApiRateLimitPartitioner.CreateLimiter(monitor);
        var context = new DefaultHttpContext { Request = { Path = "/mcp" } };

        using var first = limiter.AttemptAcquire(context);
        using var second = limiter.AttemptAcquire(context);

        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeFalse();
    }

    private static async Task<bool> InvokeAsync(HttpContext context, PartitionedRateLimiter<HttpContext> limiter)
    {
        var called = false;

        Task Next(HttpContext _)
        {
            called = true;
            return Task.CompletedTask;
        }

        var monitor = new StubOptionsMonitor<ApiRateLimitingOptions>(new ApiRateLimitingOptions { Enabled = true });
        var middleware = new RateLimitingMiddleware(Next, monitor, limiter);
        await middleware.InvokeAsync(context);
        return called;
    }

    private static PartitionedRateLimiter<HttpContext> RefusingLimiter()
    {
        var limiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetConcurrencyLimiter(string.Empty, _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 0
            }));
        limiter.AttemptAcquire(new DefaultHttpContext()).IsAcquired.ShouldBeTrue();
        return limiter;
    }
}

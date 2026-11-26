using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;
using HttpEndpoint = Microsoft.AspNetCore.Http.Endpoint;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class RateLimitingMiddlewareTests
{
    private const string ApiPath = "/api/work-orders";

    [Test]
    public async Task InvokeAsync_CallsNext_WhenEndpointHasNoRateLimitAttribute()
    {
        var context = new DefaultHttpContext { Request = { Path = ApiPath } };

        var called = await InvokeAsync(context, enabled: true, RefusingLimiter());

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_WhenLimitingDisabled()
    {
        var context = ContextWithApiPolicy(ApiPath);

        var called = await InvokeAsync(context, enabled: false, RefusingLimiter());

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_WhenPathIsNotRateLimited()
    {
        var context = ContextWithApiPolicy("/index.html");

        var called = await InvokeAsync(context, enabled: true, RefusingLimiter());

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Test]
    public async Task InvokeAsync_Answers429_WhenEveryTermAppliesAndThePermitIsRefused()
    {
        var context = ContextWithApiPolicy(ApiPath);
        context.Response.Body = new MemoryStream();

        var called = await InvokeAsync(context, enabled: true, RefusingLimiter());

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Test]
    public async Task InvokeAsync_CallsNextWithHeaders_WhenEveryTermAppliesAndThePermitIsGranted()
    {
        var context = ContextWithApiPolicy(ApiPath);
        var allowing = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetNoLimiter(string.Empty));

        var called = await InvokeAsync(context, enabled: true, allowing);

        called.ShouldBeTrue();
        context.Response.Headers.ContainsKey(RateLimitingMiddleware.HeaderLimit).ShouldBeTrue();
    }

    [Test]
    public void ShouldApply_ReturnsFalse_WhenEndpointMissing()
    {
        ApiRateLimitingDecision.EndpointUsesApiPolicy(new DefaultHttpContext()).ShouldBeFalse();
    }

    private static async Task<bool> InvokeAsync(
        HttpContext context,
        bool enabled,
        PartitionedRateLimiter<HttpContext> limiter)
    {
        var called = false;

        Task Next(HttpContext _)
        {
            called = true;
            return Task.CompletedTask;
        }

        var monitor = new StubOptionsMonitor<ApiRateLimitingOptions>(new ApiRateLimitingOptions { Enabled = enabled });
        var middleware = new RateLimitingMiddleware(Next, monitor, limiter);
        await middleware.InvokeAsync(context);
        return called;
    }

    private static PartitionedRateLimiter<HttpContext> RefusingLimiter()
    {
        var limiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetConcurrencyLimiter(string.Empty, SinglePermit));
        var held = limiter.AttemptAcquire(new DefaultHttpContext());
        held.IsAcquired.ShouldBeTrue();
        return limiter;
    }

    private static ConcurrencyLimiterOptions SinglePermit(string _) =>
        new() { PermitLimit = 1, QueueLimit = 0 };

    private static DefaultHttpContext ContextWithApiPolicy(string path)
    {
        var context = new DefaultHttpContext { Request = { Path = path } };
        var metadata = new EndpointMetadataCollection(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName));
        context.SetEndpoint(new HttpEndpoint(_ => Task.CompletedTask, metadata, "probe"));
        return context;
    }
}

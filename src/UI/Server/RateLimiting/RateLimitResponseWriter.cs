using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// The rate-limit headers on an allowed API response, and the 429 response with <c>Retry-After</c> for a refused one.
/// Both advertise the window of the same <see cref="SlidingWindowRule"/> the limiter enforces.
/// </summary>
internal static class RateLimitResponseWriter
{
    internal static void AddRateLimitHeaders(
        HttpContext context,
        PartitionedRateLimiter<HttpContext> limiter,
        ApiRateLimitingOptions opts)
    {
        var stats = limiter.GetStatistics(context);
        WriteLimitHeaders(context, opts.ToSlidingWindowRule(), stats?.CurrentAvailablePermits ?? 0);
    }

    internal static async Task WriteRateLimitedResponseAsync(HttpContext context, ApiRateLimitingOptions opts)
    {
        var rule = opts.ToSlidingWindowRule();
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = rule.WindowSeconds.ToString(NumberFormatInfo.InvariantInfo);
        WriteLimitHeaders(context, rule, availablePermits: 0);
        if (context.Features.Get<IHttpResponseFeature>()?.HasStarted != true)
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Too many requests. Please try again later.", context.RequestAborted);
        }
    }

    private static void WriteLimitHeaders(HttpContext context, SlidingWindowRule rule, long availablePermits)
    {
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit] = rule.AdvertisedPermitLimit;
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining] = SlidingWindowRule.AdvertisedRemaining(availablePermits);
        context.Response.Headers[RateLimitingMiddleware.HeaderReset] =
            DateTimeOffset.UtcNow.Add(rule.Window).ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);
    }
}

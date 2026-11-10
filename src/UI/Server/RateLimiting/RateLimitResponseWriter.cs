using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// The rate-limit headers on an allowed API response, and the 429 response with <c>Retry-After</c> for a refused one.
/// </summary>
internal static class RateLimitResponseWriter
{
    internal static void AddRateLimitHeaders(
        HttpContext context,
        PartitionedRateLimiter<HttpContext> limiter,
        ApiRateLimitingOptions opts)
    {
        var stats = limiter.GetStatistics(context);
        var remaining = stats?.CurrentAvailablePermits ?? 0;
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit] =
            opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining] =
            Math.Max(0, remaining).ToString(NumberFormatInfo.InvariantInfo);
        var window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds));
        context.Response.Headers[RateLimitingMiddleware.HeaderReset] =
            DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);
    }

    internal static async Task WriteRateLimitedResponseAsync(HttpContext context, ApiRateLimitingOptions opts)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retryAfter = Math.Max(1, opts.WindowSeconds);
        context.Response.Headers.RetryAfter = retryAfter.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit] =
            opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining] = "0";
        var window = TimeSpan.FromSeconds(retryAfter);
        context.Response.Headers[RateLimitingMiddleware.HeaderReset] =
            DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);
        if (context.Features.Get<IHttpResponseFeature>()?.HasStarted != true)
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Too many requests. Please try again later.", context.RequestAborted);
        }
    }
}

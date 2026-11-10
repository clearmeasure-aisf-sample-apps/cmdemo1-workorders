using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Enforces sliding-window limits for endpoints marked with <see cref="EnableRateLimitingAttribute"/>.
/// </summary>
public sealed class RateLimitingMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiRateLimitingOptions> optionsMonitor,
    PartitionedRateLimiter<HttpContext> limiter)
{
    public const string HeaderLimit = "X-RateLimit-Limit";
    public const string HeaderRemaining = "X-RateLimit-Remaining";
    public const string HeaderReset = "X-RateLimit-Reset";

    public async Task InvokeAsync(HttpContext context)
    {
        var options = optionsMonitor.CurrentValue;
        if (!ShouldLimit(context, options))
        {
            await next(context);
            return;
        }

        using var permit = await limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
        if (!permit.IsAcquired)
        {
            await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, options);
            return;
        }

        RateLimitResponseWriter.AddRateLimitHeaders(context, limiter, options);
        await next(context);
    }

    private static bool ShouldLimit(HttpContext context, ApiRateLimitingOptions options) =>
        RateLimitingMiddlewareRules.ShouldApply(context)
        && options.Enabled
        && ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path);
}

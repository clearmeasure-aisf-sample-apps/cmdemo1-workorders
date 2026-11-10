using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Which endpoints the rate-limiting middleware applies to: those marked with the API policy's
/// <see cref="EnableRateLimitingAttribute"/>.
/// </summary>
internal static class RateLimitingMiddlewareRules
{
    internal static bool ShouldApply(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attr = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        return attr is { PolicyName: ApiRateLimiting.PolicyName };
    }
}

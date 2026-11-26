using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// The single answer to "does API rate limiting apply to this request?", consulted by
/// <see cref="RateLimitingMiddleware"/> and by the partition factory registered in
/// <see cref="ApiRateLimitingExtensions.AddApiRateLimiting"/>. Each term is a named predicate.
/// </summary>
internal static class ApiRateLimitingDecision
{
    /// <summary>
    /// The full decision: the endpoint opts in with the API policy, limiting is enabled and the path is rate limited.
    /// </summary>
    internal static bool AppliesTo(HttpContext context, ApiRateLimitingOptions options) =>
        EndpointUsesApiPolicy(context) && LimiterAppliesTo(context, options);

    /// <summary>
    /// The terms the shared limiter decides on (it does not see endpoint metadata): enabled and a rate-limited path.
    /// </summary>
    internal static bool LimiterAppliesTo(HttpContext context, ApiRateLimitingOptions options) =>
        IsEnabled(options) && IsRateLimitedPath(context);

    /// <summary>
    /// Metadata term: the endpoint is marked with the API policy's <see cref="EnableRateLimitingAttribute"/>.
    /// </summary>
    internal static bool EndpointUsesApiPolicy(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attr = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        return attr is { PolicyName: ApiRateLimiting.PolicyName };
    }

    /// <summary>
    /// Enabled term: <see cref="ApiRateLimitingOptions.Enabled"/>.
    /// </summary>
    internal static bool IsEnabled(ApiRateLimitingOptions options) => options.Enabled;

    /// <summary>
    /// Path term: the request path is one <see cref="ApiRoutePolicy"/> rate limits.
    /// </summary>
    internal static bool IsRateLimitedPath(HttpContext context) =>
        ApiRoutePolicy.IsRateLimitedPath(context.Request.Path);
}

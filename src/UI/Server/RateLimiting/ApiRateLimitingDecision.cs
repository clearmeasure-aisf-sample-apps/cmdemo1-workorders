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
    /// The full decision: limiting is enabled, the path is rate limited, and either the endpoint opts in with the
    /// API policy or <see cref="PathBasedRateLimitGate"/> enforces the path without metadata.
    /// </summary>
    internal static bool AppliesTo(HttpContext context, ApiRateLimitingOptions options) =>
        (EndpointUsesApiPolicy(context) || PathEnforcesWithoutMetadata(context)) && LimiterAppliesTo(context, options);

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
    /// Path term: the request path is one <see cref="PathBasedRateLimitGate"/> rate limits.
    /// </summary>
    internal static bool IsRateLimitedPath(HttpContext context) =>
        PathBasedRateLimitGate.IsRateLimitedPath(context.Request.Path);

    /// <summary>
    /// Path-enforced term: <see cref="PathBasedRateLimitGate"/> limits the path even without endpoint metadata.
    /// </summary>
    internal static bool PathEnforcesWithoutMetadata(HttpContext context) =>
        PathBasedRateLimitGate.EnforcesWithoutMetadata(context.Request.Path);
}

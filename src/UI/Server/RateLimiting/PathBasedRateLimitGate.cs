namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Decides API rate-limit enforcement from the request path alone, so a route that carries no
/// <see cref="Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute"/> cannot slip past the limiter.
/// Consulted by <see cref="ApiRateLimitingDecision"/> for both <see cref="RateLimitingMiddleware"/> and the
/// partition factory in <see cref="ApiRateLimitPartitioner"/>.
/// </summary>
internal static class PathBasedRateLimitGate
{
    /// <summary>
    /// True when the path is one the limiter covers: every path <see cref="ApiRoutePolicy.IsRateLimitedPath"/>
    /// rate limits plus the <c>/mcp</c> endpoint.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsRateLimitedPath(PathString path) =>
        ApiRoutePolicy.IsRateLimitedPath(path) || ApiRoutePolicy.IsMcpPath(path);

    /// <summary>
    /// True when the path is limited whatever the endpoint metadata says: the Blazor single-API routes (the main
    /// bus, including every remotable request) and the <c>/mcp</c> endpoint. Other API routes keep the
    /// <see cref="Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute"/> opt-in.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool EnforcesWithoutMetadata(PathString path) =>
        ApiRoutePolicy.IsBlazorSingleApiPath(path) || ApiRoutePolicy.IsMcpPath(path);
}

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/health</c> (load-balancer probe) and <c>GET api/health/detailed</c> (every registered health check).
/// </summary>
public static class DetailedHealthEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("health", Get);
        api.MapGet("health/detailed", GetDetailed);
    }

    /// <summary>
    /// Lightweight liveness/readiness probe used by the load balancer and container orchestrator.
    /// Returns a simple timestamp JSON payload. Performs no deep dependency checks.
    /// </summary>
    public static IResult Get(HttpContext context, TimeProvider timeProvider) =>
        ConditionalGetEtag.ConditionalJson(context, SimpleHealthResponseBuilder.Build(timeProvider));

    /// <summary>
    /// Runs all registered <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck"/> implementations.
    /// Intended for monitoring dashboards; not suitable for the load-balancer probe path. The ETag ignores volatile
    /// fields so repeated polls can answer 304.
    /// </summary>
    public static async Task<IResult> GetDetailed(
        HttpContext context,
        IDetailedHealthReportProvider detailedHealthReportProvider,
        CancellationToken cancellationToken)
    {
        var payload = await detailedHealthReportProvider.GetReportAsync(cancellationToken);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(DetailedHealthEtagFingerprint.FromReport(payload));
        return ConditionalGetEtag.Conditional(context, etag, () => ConditionalGetEtag.JsonContent(payload));
    }
}

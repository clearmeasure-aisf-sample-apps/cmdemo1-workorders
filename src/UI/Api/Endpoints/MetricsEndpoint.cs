namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/metrics/summary</c>: request counts and uptime since the process started.
/// </summary>
public static class MetricsEndpoint
{
    public static void Map(IEndpointRouteBuilder api) => api.MapGet("metrics/summary", GetSummary);

    public static IResult GetSummary(
        HttpContext context,
        TimeProvider timeProvider,
        IHttpRequestMetricsCounter requestCounter) =>
        ConditionalGetEtag.ConditionalJson(context, MetricsSummaryBuilder.Build(timeProvider, requestCounter));
}

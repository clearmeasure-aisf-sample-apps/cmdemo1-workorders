using Asp.Versioning;
using Asp.Versioning.Builder;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Maps every HTTP API endpoint as a minimal API. Contract endpoints answer at <c>api/...</c> and at the versioned twin
/// <c>api/v{version}/...</c> (version 1.0, assumed when unspecified) and are rate limited unless noted.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Maps the API under <paramref name="api"/>, so conventions added to it (timeouts, CORS) reach every endpoint.
    /// </summary>
    public static void MapApiEndpoints(this IEndpointRouteBuilder api)
    {
        var versions = api.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).ReportApiVersions().Build();
        MapContract(api.MapGroup("api"), versions);
        MapContract(api.MapGroup(ApiRoutes.VersionedApiPrefix), versions);
        WeatherForecastEndpoint.Map(Versioned(api.MapGroup(""), versions).RequireRateLimiting(ApiRateLimiting.PolicyName));
        DiagnosticEndpoints.Map(api);
    }

    /// <summary>The endpoints of the versioned contract under one route prefix.</summary>
    private static void MapContract(RouteGroupBuilder prefix, ApiVersionSet versions)
    {
        var contract = Versioned(prefix, versions);
        WorkOrderStatusCountsEndpoint.Map(contract);

        var limited = contract.MapGroup("").RequireRateLimiting(ApiRateLimiting.PolicyName);
        DetailedHealthEndpoint.Map(limited);
        DiagnosticsEndpoint.Map(limited);
        EchoEndpoint.Map(limited);
        EnvironmentStatusEndpoint.Map(limited);
        FeatureFlagsEndpoint.Map(limited);
        MetricsEndpoint.Map(limited);
        PingEndpoint.Map(limited);
        TimeEndpoint.Map(limited);
        VersionEndpoint.Map(limited);
        WorkOrdersBulkImportEndpoint.Map(limited);
        MapTools(limited.MapGroup("tools"));
    }

    private static void MapTools(RouteGroupBuilder tools)
    {
        TimestampConverterEndpoint.Map(tools);
        ToolsBase64Endpoint.Map(tools);
        ToolsCaseEndpoint.Map(tools);
        ToolsDueDateCheckEndpoint.Map(tools);
        ToolsGuidGeneratorEndpoint.Map(tools);
        ToolsHashEndpoint.Map(tools);
        ToolsRandomEndpoint.Map(tools);
        ToolsSlugifyEndpoint.Map(tools);
        ToolsUppercaseEndpoint.Map(tools);
        ToolsWordCountEndpoint.Map(tools);
        ToolsWorkOrderStatusesEndpoint.Map(tools);
    }

    /// <summary>Puts <paramref name="group"/> in contract version 1.0.</summary>
    public static RouteGroupBuilder Versioned(RouteGroupBuilder group, ApiVersionSet versions)
    {
        group.WithApiVersionSet(versions).HasApiVersion(new ApiVersion(1, 0));
        return group;
    }
}

using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/diagnostics</c>: runtime diagnostics (environment, uptime, feature flags) for operations and support.
/// </summary>
public static class DiagnosticsEndpoint
{
    public static void Map(IEndpointRouteBuilder api) => api.MapGet("diagnostics", Get);

    /// <summary>
    /// Returns environment name, process uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>), and
    /// the runtime feature flags from <see cref="FeatureFlagsQuery"/>.
    /// </summary>
    public static async Task<IResult> Get(IHostEnvironment hostEnvironment, TimeProvider timeProvider, IBus bus)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        var featureFlags = await bus.Send(new FeatureFlagsQuery());
        var payload = new DiagnosticsResponse(
            Environment: hostEnvironment.EnvironmentName,
            Uptime: healthSlice.Uptime,
            FeatureFlags: featureFlags.Flags);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

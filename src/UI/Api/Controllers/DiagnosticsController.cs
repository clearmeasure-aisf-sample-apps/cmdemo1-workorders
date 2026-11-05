using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime diagnostics (environment, uptime, feature flags) for operations and support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/diagnostics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/diagnostics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class DiagnosticsController(
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider,
    IFeatureFlagRuntime featureFlags) : ControllerBase
{
    /// <summary>
    /// Returns environment name, process uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>),
    /// and the feature-flag map from <see cref="IFeatureFlagRuntime"/> (the same source as <c>GET /api/features/flags</c>).
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        var payload = new DiagnosticsResponse(
            Environment: hostEnvironment.EnvironmentName,
            Uptime: healthSlice.Uptime,
            FeatureFlags: featureFlags.Snapshot());
        return ConditionalGetEtag.JsonContent(payload);
    }
}

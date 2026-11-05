using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
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
    IBus bus) : ControllerBase
{
    /// <summary>
    /// Returns environment name, process uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>), and
    /// the runtime feature flags from <see cref="FeatureFlagsQuery"/>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
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

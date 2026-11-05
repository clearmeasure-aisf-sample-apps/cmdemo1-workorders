using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature flag status for operations and support tooling, read through
/// <see cref="FeatureFlagsQuery"/> on the bus.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController(IBus bus) : ControllerBase
{
    /// <summary>
    /// Returns a JSON object of all known feature flag names mapped to enabled/disabled status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await bus.Send(new FeatureFlagsQuery());
        return ConditionalGetEtag.JsonContent(result.Flags);
    }
}

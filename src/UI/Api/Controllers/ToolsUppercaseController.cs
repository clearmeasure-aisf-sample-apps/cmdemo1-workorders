using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns submitted text in upper case using the invariant culture.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/uppercase")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/uppercase")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsUppercaseController : ControllerBase
{
    /// <summary>
    /// Returns <paramref name="request"/>.Text converted with <see cref="string.ToUpperInvariant"/>.
    /// A missing body or text yields an empty string.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(UppercaseResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] UppercaseRequest? request)
    {
        var text = request?.Text ?? string.Empty;
        return Ok(new UppercaseResponse(text.ToUpperInvariant()));
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/uppercase</c>.
/// </summary>
/// <param name="Text">Text to convert. Null or omitted is treated as empty.</param>
public record UppercaseRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/uppercase</c>.
/// </summary>
/// <param name="Text">Upper-case text.</param>
public record UppercaseResponse(string Text);

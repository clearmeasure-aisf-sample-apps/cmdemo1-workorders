using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts text elements in submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/char-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/char-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsCharCountController : ControllerBase
{
    /// <summary>
    /// Returns the text-element and non-whitespace text-element counts for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(CharCountResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] CharCountRequest? request)
    {
        var text = request?.Text ?? string.Empty;
        var characters = 0;
        var nonWhitespace = 0;
        var textElements = StringInfo.GetTextElementEnumerator(text);

        while (textElements.MoveNext())
        {
            characters++;
            if (!string.IsNullOrWhiteSpace(textElements.GetTextElement()))
            {
                nonWhitespace++;
            }
        }

        return Ok(new CharCountResponse(characters, nonWhitespace));
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/char-count</c>.
/// </summary>
public record CharCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/char-count</c>.
/// </summary>
public record CharCountResponse(int Characters, int NonWhitespace);

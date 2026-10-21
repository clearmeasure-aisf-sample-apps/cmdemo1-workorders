using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts Unicode text elements in submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/char-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/char-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsCharCountController : ControllerBase
{
    /// <summary>
    /// Returns the character and non-whitespace character counts for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(CharCountResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] CharCountRequest? request)
    {
        var text = request?.Text ?? string.Empty;
        var textElements = StringInfo.GetTextElementEnumerator(text);
        var characterCount = 0;
        var nonWhitespaceCount = 0;

        while (textElements.MoveNext())
        {
            characterCount++;
            if (!string.IsNullOrWhiteSpace(textElements.GetTextElement()))
            {
                nonWhitespaceCount++;
            }
        }

        return Ok(new CharCountResponse(characterCount, nonWhitespaceCount));
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

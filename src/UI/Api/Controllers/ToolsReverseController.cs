using System.Globalization;
using System.Net.Mime;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Reverses submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/reverse")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/reverse")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsReverseController : ControllerBase
{
    /// <summary>
    /// Returns the submitted <paramref name="request"/>.Text with its text elements reversed.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ReverseResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] ReverseRequest? request)
    {
        var text = request?.Text;
        var reversed = text is null ? string.Empty : ReverseTextElements(text);
        return Ok(new ReverseResponse(reversed));
    }

    private static string ReverseTextElements(string text)
    {
        var textElementIndexes = StringInfo.ParseCombiningCharacters(text);
        var result = new StringBuilder(text.Length);

        for (var i = textElementIndexes.Length - 1; i >= 0; i--)
        {
            var start = textElementIndexes[i];
            var nextStart = i == textElementIndexes.Length - 1 ? text.Length : textElementIndexes[i + 1];
            result.Append(text, start, nextStart - start);
        }

        return result.ToString();
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/reverse</c>.
/// </summary>
public record ReverseRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/reverse</c>.
/// </summary>
public record ReverseResponse(string Reversed);

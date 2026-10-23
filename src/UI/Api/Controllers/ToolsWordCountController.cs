using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts words in submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/word-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/word-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsWordCountController : ControllerBase
{
    /// <summary>
    /// Returns the word count for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] WordCountRequest? request)
    {
        var wordCount = request?.Text?
            .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries)
            .Length ?? 0;

        return Ok(new WordCountResponse(wordCount));
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountResponse(int Words);

using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts words, characters, and lines in submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/word-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/word-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsWordCountController : ControllerBase
{
    /// <summary>
    /// Returns word, character, and line counts for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    public IActionResult Post([FromBody] WordCountRequest? request)
    {
        var text = request?.Text;
        return Ok(new WordCountResponse(CountWords(text), CountCharacters(text), CountLines(text)));
    }

    private static int CountWords(string? text) =>
        text?.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

    private static int CountCharacters(string? text) => text?.Length ?? 0;

    private static int CountLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n').Length;
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/word-count</c>: words, characters, and lines.
/// </summary>
public record WordCountResponse(int Words, int Characters, int Lines);

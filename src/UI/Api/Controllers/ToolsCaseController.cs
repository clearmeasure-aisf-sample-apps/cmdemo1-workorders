using System.Net.Mime;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts text between upper, lower, title, kebab, and snake case.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/case")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/case")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsCaseController : ControllerBase
{
    /// <summary>
    /// Returns <paramref name="request"/>.Text converted to the case named by <paramref name="request"/>.To.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(CaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] CaseRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body field 'text' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var to = request.To?.Trim().ToLowerInvariant();
        return to switch
        {
            "upper" => Ok(new CaseResponse(request.Text.ToUpperInvariant())),
            "lower" => Ok(new CaseResponse(request.Text.ToLowerInvariant())),
            "title" => Ok(new CaseResponse(ToTitleCase(request.Text))),
            "kebab" => Ok(new CaseResponse(ToSeparated(request.Text, '-'))),
            "snake" => Ok(new CaseResponse(ToSeparated(request.Text, '_'))),
            _ => UnknownTo()
        };
    }

    private IActionResult UnknownTo() =>
        Problem(
            detail: "JSON body field 'to' must be 'upper', 'lower', 'title', 'kebab', or 'snake'.",
            statusCode: StatusCodes.Status400BadRequest);

    private static string ToTitleCase(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var words = Regex.Split(trimmed, @"\s+");
        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index];
            words[index] = char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        }

        return string.Join(' ', words);
    }

    private static string ToSeparated(string text, char separator)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var converted = Regex.Replace(text, "[^a-zA-Z0-9]+", separator.ToString());
        converted = converted.Trim(separator);
        return converted.ToLowerInvariant();
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/case</c>.
/// </summary>
/// <param name="Text">Text to convert.</param>
/// <param name="To"><c>upper</c>, <c>lower</c>, <c>title</c>, <c>kebab</c>, or <c>snake</c>; case-insensitive.</param>
public record CaseRequest(string? Text, string? To);

/// <summary>
/// JSON payload for <c>POST /api/tools/case</c>.
/// </summary>
/// <param name="Text">Converted text.</param>
public record CaseResponse(string Text);

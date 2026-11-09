using System.Net.Mime;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts text to URL slugs for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/slugify")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/slugify")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsSlugifyController : ControllerBase
{
    private const int MaxSlugLength = 80;

    /// <summary>
    /// Returns a URL slug derived from the submitted <paramref name="request"/>.Text.
    /// The slug is at most 80 characters and does not end with <c>-</c>.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(SlugifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] SlugifyRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body field 'text' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var text = request.Text;
        var slug = GenerateSlug(text);
        return Ok(new SlugifyResponse(slug));
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var slug = Regex.Replace(text, "[^a-zA-Z0-9]+", "-");
        slug = slug.Trim('-');
        slug = slug.ToLowerInvariant();
        if (slug.Length <= MaxSlugLength)
        {
            return slug;
        }

        return slug.Substring(0, MaxSlugLength).TrimEnd('-');
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/slugify</c>.
/// </summary>
public record SlugifyRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/slugify</c>.
/// </summary>
public record SlugifyResponse(string Slug);

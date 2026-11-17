using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Converts text to URL slugs for operators and integrations.
/// </summary>
public static class ToolsSlugifyEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapPost("slugify", Post).AllowAnonymous();

    private const int MaxSlugLength = 80;

    /// <summary>
    /// Returns a URL slug derived from the submitted <paramref name="request"/>.Text.
    /// The slug is at most 80 characters and does not end with <c>-</c>.
    /// </summary>
    public static IResult Post(SlugifyRequest? request)
    {
        if (request?.Text is null)
        {
            return ApiProblems.BadRequest("JSON body field 'text' is required.");
        }

        var text = request.Text;
        var slug = GenerateSlug(text);
        return TypedResults.Ok(new SlugifyResponse(slug));
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

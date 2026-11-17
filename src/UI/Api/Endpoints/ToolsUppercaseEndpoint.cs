
namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Returns submitted text in upper case using the invariant culture.
/// </summary>
public static class ToolsUppercaseEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapPost("uppercase", Post).AllowAnonymous();

    /// <summary>
    /// Returns <paramref name="request"/>.Text converted with <see cref="string.ToUpperInvariant"/>.
    /// A missing body or text yields an empty string.
    /// </summary>
    public static IResult Post(UppercaseRequest? request)
    {
        var text = request?.Text ?? string.Empty;
        return TypedResults.Ok(new UppercaseResponse(text.ToUpperInvariant()));
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

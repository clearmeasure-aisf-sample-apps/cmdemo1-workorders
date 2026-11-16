using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Encodes and decodes Base64 text for operators and integrations.
/// </summary>
public static class ToolsBase64Endpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapPost("base64", Post).AllowAnonymous();

    /// <summary>
    /// Encodes or decodes <paramref name="request"/>.Text as Base64 (UTF-8).
    /// When <c>mode</c> is omitted or null, defaults to encode.
    /// </summary>
    public static IResult Post(Base64Request? request)
    {
        if (request?.Text is null || request.Text.Length == 0)
        {
            return ApiProblems.BadRequest("JSON body field 'text' is required and must be non-empty.");
        }

        var mode = string.IsNullOrWhiteSpace(request.Mode)
            ? "encode"
            : request.Mode.Trim().ToLowerInvariant();

        return mode switch
        {
            "encode" => TypedResults.Ok(new Base64Response(
                Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Text)))),
            "decode" => Decode(request.Text),
            _ => ApiProblems.BadRequest("JSON body field 'mode' must be 'encode' or 'decode'.")
        };
    }

    private static IResult Decode(string text)
    {
        try
        {
            var bytes = Convert.FromBase64String(text);
            return TypedResults.Ok(new Base64Response(Encoding.UTF8.GetString(bytes)));
        }
        catch (FormatException)
        {
            return ApiProblems.BadRequest("JSON body field 'text' is not valid Base64.");
        }
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/base64</c>.
/// </summary>
/// <param name="Text">Plain text to encode, or Base64 to decode.</param>
/// <param name="Mode"><c>encode</c> (default) or <c>decode</c>; case-insensitive.</param>
public record Base64Request(string? Text, string? Mode = null);

/// <summary>
/// JSON payload for <c>POST /api/tools/base64</c>.
/// </summary>
/// <param name="Result">Encoded Base64 string or decoded UTF-8 text.</param>
public record Base64Response(string Result);


namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Counts words, characters, and lines in submitted text for operators and integrations.
/// </summary>
public static class ToolsWordCountEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapPost("word-count", Post).AllowAnonymous();

    /// <summary>
    /// Returns word, character, and line counts for the submitted <paramref name="request"/>.Text.
    /// </summary>
    public static IResult Post(WordCountRequest? request)
    {
        var text = request?.Text;
        return TypedResults.Ok(new WordCountResponse(CountWords(text), CountCharacters(text), CountLines(text)));
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

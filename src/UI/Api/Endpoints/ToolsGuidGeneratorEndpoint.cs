
namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Generates one or more GUIDs for operators and integrations.
/// </summary>
public static class ToolsGuidGeneratorEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapPost("guid-generator", Post).AllowAnonymous();

    private const int DefaultCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Returns a JSON array of newly generated GUIDs in D format.
    /// Optional <paramref name="count"/> defaults to 1 and must be between 1 and 100 inclusive.
    /// </summary>
    public static IResult Post(int count = DefaultCount)
    {
        if (count < DefaultCount || count > MaxCount)
        {
            return ApiProblems.BadRequest($"Query parameter 'count' must be between {DefaultCount} and {MaxCount} inclusive.");
        }

        var guids = new string[count];
        for (var i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return TypedResults.Ok(guids);
    }
}

using System.Globalization;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Generates configurable random values for operators and integrations.
/// </summary>
public static class ToolsRandomEndpoint
{
    public static void Map(IEndpointRouteBuilder tools)
    {
        tools.MapGet("random", Get).AllowAnonymous();
        tools.MapPost("random", Post).AllowAnonymous();
    }

    private const string AlphanumericChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int AlphanumericLength = 12;

    private static readonly Dictionary<string, Func<string>> Generators =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["number"] = () => Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["string"] = GenerateAlphanumeric,
            ["uuid"] = static () => Guid.NewGuid().ToString("D"),
            ["color"] = static () => $"#{Random.Shared.Next(0x1000000):X6}"
        };

    /// <summary>
    /// Returns a random value as plain text for the requested <paramref name="type"/>.
    /// Supported types: <c>number</c>, <c>string</c>, <c>uuid</c>, <c>color</c>.
    /// </summary>
    public static IResult Get(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return ApiProblems.BadRequest("Query parameter 'type' is required. Supported values: number, string, uuid, color.");
        }

        if (!Generators.TryGetValue(type.Trim(), out var generator))
        {
            return ApiProblems.BadRequest($"Unknown type '{type}'. Supported values: number, string, uuid, color.");
        }

        return PlainText.Ok(generator());
    }

    /// <summary>
    /// Returns a random integer in the inclusive range from <paramref name="min"/> to <paramref name="max"/> as plain text.
    /// </summary>
    /// <param name="min">Inclusive lower bound. Required.</param>
    /// <param name="max">Inclusive upper bound. Required.</param>
    public static IResult Post(int? min, int? max)
    {
        if (min is not null && max is not null)
        {
            if (min.Value > max.Value)
            {
                return ApiProblems.BadRequest("Query parameter 'min' must be less than or equal to 'max'.");
            }

            var value = min.Value == max.Value ? min.Value : NextInclusive(min.Value, max.Value);
            return PlainText.Ok(value.ToString(CultureInfo.InvariantCulture));
        }

        return ApiProblems.BadRequest("Query parameters 'min' and 'max' are required.");
    }

    private static int NextInclusive(int lower, int upper)
    {
        var span = (long)upper - lower + 1;
        return (int)(lower + Random.Shared.NextInt64(span));
    }

    private static string GenerateAlphanumeric()
    {
        var buffer = new StringBuilder(AlphanumericLength);
        for (var i = 0; i < AlphanumericLength; i++)
        {
            buffer.Append(AlphanumericChars[Random.Shared.Next(AlphanumericChars.Length)]);
        }

        return buffer.ToString();
    }
}

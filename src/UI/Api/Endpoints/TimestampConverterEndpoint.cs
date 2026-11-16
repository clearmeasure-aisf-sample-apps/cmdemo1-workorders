using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/tools/timestamp-converter?unix=…|iso=…</c>: converts one instant between Unix seconds, ISO-8601 and a
/// human-readable UTC form.
/// </summary>
public static class TimestampConverterEndpoint
{
    private const string IsoOutputFormat = "yyyy-MM-ddTHH:mm:ssZ";
    private const string HumanOutputFormat = "dddd, dd MMMM yyyy HH:mm:ss UTC";

    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapGet("timestamp-converter", Get).AllowAnonymous();

    /// <summary>Exactly one of <paramref name="unix"/> or <paramref name="iso"/> must be present.</summary>
    public static IResult Get(HttpRequest request, string? unix, string? iso)
    {
        var queryError = ValidateQueryKeys(request.Query);
        if (queryError != null)
        {
            return ApiProblems.BadRequest(queryError);
        }

        var parseResult = ParseInstant(request.Query.ContainsKey("unix"), unix, iso);
        if (!parseResult.Success)
        {
            return ApiProblems.BadRequest(parseResult.Error);
        }

        return TypedResults.Ok(BuildResponse(parseResult.Instant));
    }

    private static string? ValidateQueryKeys(IQueryCollection query)
    {
        var hasUnix = query.ContainsKey("unix");
        var hasIso = query.ContainsKey("iso");

        if (!hasUnix && !hasIso)
        {
            return "Provide exactly one of query parameters 'unix' or 'iso'.";
        }

        if (hasUnix && hasIso)
        {
            return "Provide exactly one of query parameters 'unix' or 'iso', not both.";
        }

        return null;
    }

    private static ParseInstantResult ParseInstant(bool hasUnix, string? unix, string? iso)
    {
        if (hasUnix)
        {
            return TryParseUnixSeconds(unix, out var instant)
                ? ParseInstantResult.FromInstant(instant)
                : ParseInstantResult.FromError(
                    "Query parameter 'unix' must be a 64-bit integer Unix timestamp in seconds.");
        }

        return TryParseIso(iso, out var parsedInstant)
            ? ParseInstantResult.FromInstant(parsedInstant)
            : ParseInstantResult.FromError(
                "Query parameter 'iso' must be a valid ISO-8601 date/time string.");
    }

    private static bool TryParseUnixSeconds(string? unix, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(unix))
        {
            return false;
        }

        if (!long.TryParse(unix.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        try
        {
            instant = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryParseIso(string? iso, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(iso))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                iso.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out instant))
        {
            return false;
        }

        instant = instant.ToUniversalTime();
        return true;
    }

    private static TimestampConverterResponse BuildResponse(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new TimestampConverterResponse(
            Unix: utc.ToUnixTimeSeconds(),
            Iso: utc.ToString(IsoOutputFormat, CultureInfo.InvariantCulture),
            Human: utc.ToString(HumanOutputFormat, CultureInfo.InvariantCulture));
    }

    private readonly struct ParseInstantResult
    {
        private ParseInstantResult(bool success, DateTimeOffset instant, string? error)
        {
            Success = success;
            Instant = instant;
            Error = error;
        }

        public bool Success { get; }
        public DateTimeOffset Instant { get; }
        public string? Error { get; }

        public static ParseInstantResult FromInstant(DateTimeOffset instant) =>
            new(true, instant, null);

        public static ParseInstantResult FromError(string error) =>
            new(false, default, error);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
/// <param name="Unix">Unix epoch seconds (UTC).</param>
/// <param name="Iso">ISO-8601 UTC string, second precision (e.g. <c>2023-11-14T22:13:20Z</c>).</param>
/// <param name="Human">Human-readable UTC display (e.g. <c>Tuesday, 14 November 2023 22:13:20 UTC</c>).</param>
public record TimestampConverterResponse(long Unix, string Iso, string Human);


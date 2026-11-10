namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// The first successful response to an idempotent request: status, end-to-end headers and body, replayed for every
/// duplicate of the request.
/// </summary>
internal sealed record IdempotentResponseSnapshot(int StatusCode, Dictionary<string, string[]> Headers, byte[] Body)
{
    private static readonly HashSet<string> HopByHopHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    public static IdempotentResponseSnapshot Capture(HttpResponse response, byte[] body) =>
        new(response.StatusCode,
            response.Headers
                .Where(header => !HopByHopHeaderNames.Contains(header.Key))
                .ToDictionary(
                    header => header.Key,
                    header => header.Value.ToArray().Select(static value => value ?? string.Empty).ToArray(),
                    StringComparer.OrdinalIgnoreCase),
            body);

    public async Task ReplayAsync(HttpContext context)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCode;
        foreach (var (name, values) in Headers.Where(header =>
                     !string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.Headers[name] = values;
        }

        context.Response.ContentLength = Body.Length;
        await context.Response.Body.WriteAsync(Body, context.RequestAborted);
    }
}

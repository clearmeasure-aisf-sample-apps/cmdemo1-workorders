using System.Net;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/echo</c>: reflects key properties of the incoming HTTP request for debugging and client diagnostics.
/// </summary>
public static class EchoEndpoint
{
    internal const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> SensitiveHeaderNames =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "Authorization",
        "X-Api-Key",
        "Cookie",
        "Set-Cookie"
    ];

    public static void Map(IEndpointRouteBuilder api) => api.MapGet("echo", Get).AllowAnonymous();

    /// <summary>
    /// Returns a JSON object reflecting method, path, query, host, protocol, remote IP, and headers
    /// (sensitive header values redacted).
    /// </summary>
    public static IResult Get(HttpContext context) => TypedResults.Ok(BuildEchoResponse(context));

    private static EchoResponse BuildEchoResponse(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = SensitiveHeaderNames.Contains(header.Key)
                ? RedactedValue
                : header.Value.ToString();
        }

        return new EchoResponse
        {
            Method = request.Method,
            Path = request.Path.Value ?? string.Empty,
            QueryString = request.QueryString.HasValue ? request.QueryString.Value! : string.Empty,
            Scheme = request.Scheme,
            Host = request.Host.Value ?? string.Empty,
            Protocol = request.Protocol,
            RemoteIpAddress = FormatRemoteIp(httpContext.Connection.RemoteIpAddress),
            Headers = headers
        };
    }

    private static string? FormatRemoteIp(IPAddress? address) =>
        address is null ? null : address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public record EchoResponse
{
    public required string Method { get; init; }

    public required string Path { get; init; }

    public required string QueryString { get; init; }

    public required string Scheme { get; init; }

    public required string Host { get; init; }

    public required string Protocol { get; init; }

    public required string? RemoteIpAddress { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }
}

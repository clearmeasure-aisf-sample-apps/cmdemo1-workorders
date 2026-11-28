using System.Diagnostics.CodeAnalysis;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The single answer to "is this an API path?" in this assembly, and the API-key gate's question
/// (<see cref="RequiresApiKey"/>) over a parsed <see cref="ApiPath"/>. Consumed by the API-key gate and by the
/// API rate-limiting, timeout and idempotency gates, and the place where the two path sets are deliberately
/// different: the API-key gate decides on the <c>/api</c> tree alone, while the rate-limiting, timeout and
/// idempotency gates cover that tree plus the Blazor single-API routes, which are API traffic but are not
/// mounted under <c>/api</c>. Path literals are kept here so the classification cannot drift per gate.
/// </summary>
internal static class ApiRoutePolicy
{
    /// <summary>
    /// The prefix every versioned and unversioned API controller route is mounted under.
    /// </summary>
    private static readonly PathString ApiPrefix = new("/api");

    /// <summary>
    /// The Model Context Protocol endpoint, a machine client route mounted outside <see cref="ApiPrefix"/>.
    /// </summary>
    private static readonly PathString McpPath = new("/mcp");

    /// <summary>
    /// The legacy Blazor single-API route, mounted at the application root rather than under <see cref="ApiPrefix"/>.
    /// </summary>
    private static readonly PathString BlazorSingleApiPath = new("/" + WebServiceApiRoutes.LegacyRelativeUrl);

    /// <summary>
    /// The versioned Blazor single-API route, the v1.0 sibling of <see cref="BlazorSingleApiPath"/>.
    /// </summary>
    private static readonly PathString BlazorSingleApiPathV1 = new("/" + WebServiceApiRoutes.VersionedRelativeUrl);

    /// <summary>
    /// True when the path is under the <c>/api</c> tree, compared case-insensitively.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsApiPath(PathString path) => RequestPathPrefix.IsUnder(path, ApiPrefix);

    /// <summary>
    /// Adapter for callers holding the raw request path value rather than a <see cref="PathString"/>: a null,
    /// empty or unrooted value is not an API path instead of throwing.
    /// </summary>
    /// <param name="pathValue">The raw request path value.</param>
    internal static bool IsApiPath([NotNullWhen(true)] string? pathValue) =>
        RequestPathPrefix.IsUnder(pathValue, ApiPrefix);

    /// <summary>
    /// The API-key gate's Specification over <see cref="ApiPath"/>: true when the scheme is configured and the path
    /// is under the <c>/api</c> tree but is not one of the anonymous public leaves. A null, empty or unrooted value
    /// does not require an API key instead of throwing.
    /// </summary>
    /// <param name="pathValue">The raw request path value.</param>
    /// <param name="options">The API-key options; an unconfigured scheme never requires a key.</param>
    internal static bool RequiresApiKey(string? pathValue, ApiKeyAuthenticationOptions options) =>
        options.IsConfigured && ApiPath.Parse(pathValue) is { IsApi: true, IsPublic: false };

    /// <summary>
    /// True when the path is one of the Blazor single-API routes, compared case-insensitively.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsBlazorSingleApiPath(PathString path) =>
        RequestPathPrefix.IsUnder(path, BlazorSingleApiPath) || RequestPathPrefix.IsUnder(path, BlazorSingleApiPathV1);

    /// <summary>
    /// The <c>/api</c> tree plus the Blazor single-API routes named here - the set the rate limiter covers.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsRateLimitedPath(PathString path) => IsApiPath(path) || IsBlazorSingleApiPath(path);

    /// <summary>
    /// A machine client path: the <c>/api</c> tree or the <c>/mcp</c> endpoint. These requests get RFC 7807
    /// ProblemDetails bodies for errors and a plain 404 instead of the Blazor <c>index.html</c> fallback.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsMachineClientPath(PathString path) => IsApiPath(path) || IsMcpPath(path);

    /// <summary>
    /// True when the path is the <c>/mcp</c> endpoint or below it, compared case-insensitively.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsMcpPath(PathString path) => RequestPathPrefix.IsUnder(path, McpPath);
}

using System.Diagnostics.CodeAnalysis;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The single answer to "is this an API path?" in this assembly. Consumed by the API-key gate and by the
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
    internal static bool IsApiPath(PathString path) =>
        path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Adapter for callers holding the raw request path value rather than a <see cref="PathString"/>: a null,
    /// empty or unrooted value is not an API path instead of throwing.
    /// </summary>
    /// <param name="pathValue">The raw request path value.</param>
    internal static bool IsApiPath([NotNullWhen(true)] string? pathValue) =>
        !string.IsNullOrEmpty(pathValue) && pathValue.StartsWith('/') && IsApiPath(new PathString(pathValue));

    /// <summary>
    /// True when the path is one of the Blazor single-API routes, compared case-insensitively.
    /// </summary>
    /// <param name="path">The request path.</param>
    private static bool IsBlazorSingleApiPath(PathString path) =>
        path.StartsWithSegments(BlazorSingleApiPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(BlazorSingleApiPathV1, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The <c>/api</c> tree plus the Blazor single-API routes named here - the set the rate limiter covers.
    /// </summary>
    /// <param name="path">The request path.</param>
    internal static bool IsRateLimitedPath(PathString path) => IsApiPath(path) || IsBlazorSingleApiPath(path);
}

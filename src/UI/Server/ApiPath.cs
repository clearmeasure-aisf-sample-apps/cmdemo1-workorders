namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// An immutable request path parsed once into the parts the API-key gate asks about: whether it is under the
/// <c>/api</c> tree, its optional version segment, its leaf below the version, and whether that leaf is one of
/// the anonymous public endpoints.
/// </summary>
internal sealed class ApiPath
{
    /// <summary>
    /// The anonymous API leaves: open diagnostics and read-only tools that never require an <c>X-Api-Key</c>.
    /// </summary>
    private static readonly HashSet<string> PublicLeaves = new(StringComparer.OrdinalIgnoreCase)
    {
        "version",
        "time",
        "ping",
        "tools/work-order-statuses",
        "tools/due-date-check",
        "work-orders/status-counts",
        "health",
        "health/detailed"
    };

    private static readonly ApiPath NotApi = new();

    private ApiPath()
    {
    }

    private ApiPath(string[] segments)
    {
        IsApi = true;
        Version = ResolveVersion(segments);
        Leaf = ResolveLeaf(segments, Version);
    }

    /// <summary>True when the path is under the <c>/api</c> tree (<see cref="ApiRoutePolicy.IsApiPath(string?)"/>).</summary>
    public bool IsApi { get; }

    /// <summary>
    /// The version segment (such as <c>v1.0</c>) when the path has a second segment starting with <c>v</c> followed
    /// by at least one more segment; otherwise <see langword="null"/>.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// The segments below <c>/api</c> and the optional version, joined with <c>/</c>; <see langword="null"/> when the
    /// path is not an API path or names no segment below <c>/api</c>.
    /// </summary>
    public string? Leaf { get; }

    /// <summary>True when <see cref="Leaf"/> is one of the anonymous public API leaves, compared case-insensitively.</summary>
    public bool IsPublic => Leaf is not null && PublicLeaves.Contains(Leaf);

    /// <summary>
    /// Parses a raw request path value. A null, empty, unrooted or non-<c>/api</c> value parses to a path that is not
    /// an API path instead of throwing.
    /// </summary>
    /// <param name="pathValue">The raw request path value.</param>
    public static ApiPath Parse(string? pathValue)
    {
        if (!ApiRoutePolicy.IsApiPath(pathValue))
        {
            return NotApi;
        }

        return new ApiPath(pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? ResolveVersion(string[] segments)
    {
        var hasSegmentAfterVersion = segments.Length > 2;
        return hasSegmentAfterVersion && segments[1].StartsWith('v') ? segments[1] : null;
    }

    private static string? ResolveLeaf(string[] segments, string? version)
    {
        var leafStart = version is null ? 1 : 2;
        return segments.Length > leafStart ? string.Join('/', segments.Skip(leafStart)) : null;
    }
}

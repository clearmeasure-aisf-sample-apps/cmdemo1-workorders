using System.Diagnostics.CodeAnalysis;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The one segment-prefix matching dialect for request paths in this assembly: a path is under a prefix when it
/// equals the prefix or continues it with a <c>/</c> segment, compared case-insensitively
/// (<see cref="PathString.StartsWithSegments(PathString, StringComparison)"/>).
/// </summary>
internal static class RequestPathPrefix
{
    /// <summary>True when <paramref name="path"/> is the <paramref name="prefix"/> path or a segment below it.</summary>
    /// <param name="path">The request path.</param>
    /// <param name="prefix">The rooted prefix path.</param>
    internal static bool IsUnder(PathString path, PathString prefix) =>
        path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Adapter for callers holding the raw request path value rather than a <see cref="PathString"/>: a null,
    /// empty or unrooted value is not under any prefix instead of throwing.
    /// </summary>
    /// <param name="pathValue">The raw request path value.</param>
    /// <param name="prefix">The rooted prefix path.</param>
    internal static bool IsUnder([NotNullWhen(true)] string? pathValue, PathString prefix) =>
        !string.IsNullOrEmpty(pathValue) && pathValue.StartsWith('/') && IsUnder(new PathString(pathValue), prefix);
}

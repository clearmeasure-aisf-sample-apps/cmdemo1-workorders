namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiPublicPathRules
{
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

    internal static bool TryGetLeafSegment(string pathValue, out string leaf)
    {
        leaf = string.Empty;
        var segments = pathValue.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        leaf = ResolveApiLeaf(segments);
        return leaf.Length > 0;
    }

    private static string ResolveApiLeaf(string[] segments)
    {
        if (segments.Length == 2)
        {
            return segments[1];
        }

        return segments[1].StartsWith('v')
            ? string.Join('/', segments.Skip(2))
            : string.Join('/', segments.Skip(1));
    }

    internal static bool IsPublicApiPath(string pathValue) =>
        TryGetLeafSegment(pathValue, out var leaf) && IsPublicLeaf(leaf);

    internal static bool IsPublicLeaf(string leaf) => PublicLeaves.Contains(leaf);
}

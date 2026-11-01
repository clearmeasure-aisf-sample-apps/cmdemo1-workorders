namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Decides whether a navigation queue link (a search URL with a query string) is the current page.
/// <c>NavLink</c> ignores the query string, so every queue link would look active on any search URL;
/// this compares the path and the query parameters (order-insensitive).
/// </summary>
public static class NavQueueMatcher
{
    /// <summary>
    /// True when <paramref name="href"/>, resolved against <paramref name="baseUri"/>, has the same
    /// path and query parameters as <paramref name="currentUri"/>.
    /// </summary>
    public static bool IsActive(string currentUri, string baseUri, string href)
    {
        var current = new Uri(currentUri);
        var target = new Uri(new Uri(baseUri), href);
        return SamePath(current, target) && NormalizeQuery(current.Query) == NormalizeQuery(target.Query);
    }

    private static bool SamePath(Uri left, Uri right) =>
        string.Equals(left.AbsolutePath.TrimEnd('/'), right.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeQuery(string query) =>
        string.Join("&", query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .Order(StringComparer.OrdinalIgnoreCase));
}

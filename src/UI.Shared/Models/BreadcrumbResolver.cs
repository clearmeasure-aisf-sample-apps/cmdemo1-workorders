namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>One breadcrumb: its text and, unless it is the current page, the link it points to.</summary>
public sealed record Breadcrumb(string Text, string? Href);

/// <summary>
/// Derives the top-bar breadcrumb trail from the current route, for example
/// <c>workorder/search</c> → Work Orders / Search.
/// </summary>
public static class BreadcrumbResolver
{
    private const string WorkOrdersHref = "workorder/search";

    private static readonly Dictionary<string, Breadcrumb[]> StaticTrails = new(StringComparer.OrdinalIgnoreCase)
    {
        [""] = [new Breadcrumb("Dashboard", null)],
        ["workorder/search"] = [new Breadcrumb("Work Orders", WorkOrdersHref), new Breadcrumb("Search", null)],
        ["settings"] = [new Breadcrumb("Settings", null)],
        ["ai-agent"] = [new Breadcrumb("AI Assistant", null)],
        ["login"] = [new Breadcrumb("Sign in", null)]
    };

    /// <summary>
    /// Returns the trail for <paramref name="relativeUri"/> (relative to the app base, query allowed).
    /// The last crumb is the current page and has no link.
    /// </summary>
    public static IReadOnlyList<Breadcrumb> Resolve(string relativeUri)
    {
        var (path, query) = Split(relativeUri);
        if (StaticTrails.TryGetValue(path, out var trail))
        {
            return trail;
        }

        return path.StartsWith("workorder/manage", StringComparison.OrdinalIgnoreCase)
            ? ManageTrail(path, query)
            : [new Breadcrumb("Dashboard", ""), new Breadcrumb(Titleize(path), null)];
    }

    private static Breadcrumb[] ManageTrail(string path, string query)
    {
        var number = path.Length > "workorder/manage/".Length ? path["workorder/manage/".Length..] : null;
        var isNew = number == null || query.Contains("mode=New", StringComparison.OrdinalIgnoreCase);
        return [new Breadcrumb("Work Orders", WorkOrdersHref), new Breadcrumb(isNew ? "New" : $"WO-{number}", null)];
    }

    private static (string Path, string Query) Split(string relativeUri)
    {
        var withoutFragment = relativeUri.Split('#')[0];
        var parts = withoutFragment.Split('?', 2);
        return (parts[0].Trim('/'), parts.Length > 1 ? parts[1] : string.Empty);
    }

    private static string Titleize(string path)
    {
        var segment = path.Split('/')[0].Replace('-', ' ');
        return segment.Length == 0 ? "Page" : char.ToUpperInvariant(segment[0]) + segment[1..];
    }
}

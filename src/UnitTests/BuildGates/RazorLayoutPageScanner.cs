using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Maps layout and shell Razor files to the routable pages that render them, from
/// <c>DefaultLayout</c>, <c>@layout</c>, and layout markup (e.g. NavMenu inside MainLayout).
/// </summary>
internal static class RazorLayoutPageScanner
{
    private static readonly string[] PageRoots =
    [
        "src/UI.Shared/Pages",
        "src/UI/Client/Pages",
    ];

    private static readonly string[] LayoutSearchRoots =
    [
        "src/UI.Shared",
        "src/UI/Client",
    ];

    private const string AppRazor = "src/UI/Client/App.razor";
    private const string NavMenuRazor = "src/UI.Shared/NavMenu.razor";

    /// <summary>
    /// Checkout-relative layout or shell path → routable pages that use that chrome.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ShellPageExpansion(
        RepositoryCheckout checkout)
    {
        var pagesByLayout = PagesByLayout(checkout);
        var expansion = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (layout, pages) in pagesByLayout)
        {
            expansion[layout] = pages;
        }

        AddNavMenuExpansion(checkout, pagesByLayout, expansion);
        AddAppExpansion(checkout, pagesByLayout, expansion);
        return expansion;
    }

    /// <summary>Layout file → sorted routable pages that render with that layout.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PagesByLayout(
        RepositoryCheckout checkout)
    {
        var defaultLayout = ResolveLayoutPath(checkout, ReadDefaultLayoutName(checkout));
        var byLayout = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in RoutablePages(checkout))
        {
            AddPageToLayout(checkout, page, defaultLayout?.Name, byLayout);
        }

        return SortLayoutPages(byLayout);
    }

    private static void AddPageToLayout(
        RepositoryCheckout checkout,
        string page,
        string? defaultLayoutName,
        Dictionary<string, HashSet<string>> byLayout)
    {
        var layoutName = ReadPageLayoutName(checkout, page) ?? defaultLayoutName;
        if (layoutName is null || IsEmptyLayout(layoutName))
        {
            return;
        }

        var layoutPath = ResolveLayoutPath(checkout, layoutName)?.Path;
        if (layoutPath is null)
        {
            return;
        }

        if (!byLayout.TryGetValue(layoutPath, out var pages))
        {
            pages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            byLayout[layoutPath] = pages;
        }

        pages.Add(page);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> SortLayoutPages(
        Dictionary<string, HashSet<string>> byLayout)
    {
        return byLayout.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value.OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddNavMenuExpansion(
        RepositoryCheckout checkout,
        IReadOnlyDictionary<string, IReadOnlyList<string>> pagesByLayout,
        Dictionary<string, IReadOnlyList<string>> expansion)
    {
        var pages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (layout, layoutPages) in pagesByLayout)
        {
            if (!TagUsed(checkout.ReadText(layout), "NavMenu"))
            {
                continue;
            }

            pages.UnionWith(layoutPages);
        }

        if (pages.Count > 0)
        {
            expansion[NavMenuRazor] = pages.OrderBy(static path => path, StringComparer.Ordinal).ToArray();
        }
    }

    private static void AddAppExpansion(
        RepositoryCheckout checkout,
        IReadOnlyDictionary<string, IReadOnlyList<string>> pagesByLayout,
        Dictionary<string, IReadOnlyList<string>> expansion)
    {
        var defaultLayout = ResolveLayoutPath(checkout, ReadDefaultLayoutName(checkout));
        if (defaultLayout is null || !pagesByLayout.TryGetValue(defaultLayout.Path, out var pages))
        {
            return;
        }

        expansion[AppRazor] = pages;
    }

    private static IEnumerable<string> RoutablePages(RepositoryCheckout checkout)
    {
        foreach (var root in PageRoots)
        {
            var absolute = checkout.PathOf(root);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absolute, "*.razor", SearchOption.TopDirectoryOnly))
            {
                var relative = checkout.Relative(file);
                if (IsRoutablePage(checkout, relative))
                {
                    yield return relative;
                }
            }
        }
    }

    private static bool IsRoutablePage(RepositoryCheckout checkout, string relativeRazor)
    {
        if (HasPageDirective(checkout.ReadText(relativeRazor)))
        {
            return true;
        }

        var codeBehind = relativeRazor + ".cs";
        return File.Exists(checkout.PathOf(codeBehind)) && HasRouteAttribute(checkout.ReadText(codeBehind));
    }

    private static bool HasPageDirective(string text) =>
        Regex.IsMatch(text, @"^\s*@page\b", RegexOptions.Multiline);

    private static bool HasRouteAttribute(string text) =>
        Regex.IsMatch(text, @"\[Route\s*\(", RegexOptions.Multiline);

    private static string? ReadPageLayoutName(RepositoryCheckout checkout, string pagePath)
    {
        var match = Regex.Match(
            checkout.ReadText(pagePath),
            @"^\s*@layout\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b",
            RegexOptions.Multiline);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string? ReadDefaultLayoutName(RepositoryCheckout checkout)
    {
        if (!File.Exists(checkout.PathOf(AppRazor)))
        {
            return null;
        }

        var match = Regex.Match(
            checkout.ReadText(AppRazor),
            @"DefaultLayout\s*=\s*""@typeof\((?<name>[A-Za-z_][A-Za-z0-9_]*)\)""",
            RegexOptions.Multiline);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static LayoutRef? ResolveLayoutPath(RepositoryCheckout checkout, string? layoutName)
    {
        if (string.IsNullOrWhiteSpace(layoutName) || IsEmptyLayout(layoutName))
        {
            return null;
        }

        var fileName = layoutName + ".razor";
        foreach (var root in LayoutSearchRoots)
        {
            var absolute = checkout.PathOf(root);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absolute, fileName, SearchOption.AllDirectories))
            {
                return new LayoutRef(layoutName, checkout.Relative(file));
            }
        }

        return null;
    }

    private static bool IsEmptyLayout(string layoutName) =>
        layoutName.Equals("EmptyLayout", StringComparison.OrdinalIgnoreCase);

    private static bool TagUsed(string razorText, string componentStem) =>
        Regex.IsMatch(
            razorText,
            $@"<{Regex.Escape(componentStem)}([\s/>]|$)",
            RegexOptions.Multiline);

    private sealed record LayoutRef(string Name, string Path);
}

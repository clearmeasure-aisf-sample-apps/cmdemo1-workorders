using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Derives host pages/layouts for shared Razor components from markup tag usage,
/// resolving nested components transitively up to pages and shell layouts.
/// </summary>
internal static class RazorComponentHostScanner
{
    private static readonly string[] ComponentRoots =
    [
        "src/UI.Shared/Components",
        "src/UI.Shared/Authentication",
    ];

    private static readonly string[] UiRoots =
    [
        "src/UI.Shared",
        "src/UI/Client",
    ];

    private static readonly HashSet<string> TerminalFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MainLayout.razor",
        "NavMenu.razor",
        "PrintLayout.razor",
        "App.razor",
    };

    /// <summary>
    /// Checkout-relative component path → host pages/layouts that ultimately render it.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Scan(RepositoryCheckout checkout)
    {
        var components = ComponentFiles(checkout);
        var byStem = components.ToDictionary(
            path => Path.GetFileNameWithoutExtension(path),
            path => path,
            StringComparer.OrdinalIgnoreCase);
        var direct = DirectUsages(checkout, byStem);
        var resolved = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in components)
        {
            resolved[component] = ResolveHosts(component, direct)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray();
        }

        return resolved;
    }

    private static List<string> ComponentFiles(RepositoryCheckout checkout)
    {
        var found = new List<string>();
        foreach (var root in ComponentRoots)
        {
            var absolute = checkout.PathOf(root);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absolute, "*.razor", SearchOption.AllDirectories))
            {
                found.Add(checkout.Relative(file));
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static Dictionary<string, HashSet<string>> DirectUsages(
        RepositoryCheckout checkout,
        Dictionary<string, string> byStem)
    {
        var direct = byStem.Values.ToDictionary(
            path => path,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        foreach (var razor in UiRazorFiles(checkout))
        {
            var text = File.ReadAllText(checkout.PathOf(razor));
            foreach (var (stem, component) in byStem)
            {
                if (string.Equals(component, razor, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TagUsed(text, stem))
                {
                    direct[component].Add(razor);
                }
            }
        }

        return direct;
    }

    private static IEnumerable<string> UiRazorFiles(RepositoryCheckout checkout)
    {
        foreach (var root in UiRoots)
        {
            var absolute = checkout.PathOf(root);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absolute, "*.razor", SearchOption.AllDirectories))
            {
                var relative = checkout.Relative(file);
                if (relative.EndsWith("/_Imports.razor", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return relative;
            }
        }
    }

    private static bool TagUsed(string razorText, string componentStem)
    {
        return Regex.IsMatch(
            razorText,
            $@"<{Regex.Escape(componentStem)}([\s/>]|$)",
            RegexOptions.Multiline);
    }

    private static HashSet<string> ResolveHosts(
        string component,
        Dictionary<string, HashSet<string>> direct,
        HashSet<string>? seen = null)
    {
        seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!seen.Add(component) || !direct.TryGetValue(component, out var usages))
        {
            return [];
        }

        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var usage in usages)
        {
            AddUsageHost(usage, direct, seen, hosts);
        }

        return hosts;
    }

    private static void AddUsageHost(
        string usage,
        Dictionary<string, HashSet<string>> direct,
        HashSet<string> seen,
        HashSet<string> hosts)
    {
        if (IsTerminalHost(usage) || !direct.ContainsKey(usage))
        {
            hosts.Add(usage);
            return;
        }

        hosts.UnionWith(ResolveHosts(usage, direct, seen));
    }

    private static bool IsTerminalHost(string relativePath)
    {
        var normalized = TestToPageMap.CanonicalRazorPath(relativePath);
        return normalized.Contains("/Pages/", StringComparison.OrdinalIgnoreCase)
               || TerminalFileNames.Contains(Path.GetFileName(normalized));
    }
}

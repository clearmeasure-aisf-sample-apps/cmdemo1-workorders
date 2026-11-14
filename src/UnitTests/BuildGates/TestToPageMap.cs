using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// One page-level acceptance fixture and the Razor files it exercises
/// (<c>src/AcceptanceTests/test-to-page-map.json</c>).
/// </summary>
internal sealed class TestToPageMapEntry
{
    public string Class { get; init; } = "";

    public List<string> Pages { get; init; } = [];
}

/// <summary>Shared or shell Razor file and the host pages whose tests cover it.</summary>
internal sealed class TestToPageComponentEntry
{
    public string Path { get; init; } = "";

    public List<string> Pages { get; init; } = [];
}

/// <summary>
/// Checked-in map from page-level acceptance tests to Razor pages. A changed Razor
/// file selects its mapped fixtures for changed-UI videos (issue #1670 / item 16).
/// Shared components expand to host pages via <see cref="Components"/>.
/// </summary>
internal sealed class TestToPageMap
{
    public const string RelativePath = "src/AcceptanceTests/test-to-page-map.json";

    public List<TestToPageMapEntry> Tests { get; init; } = [];

    public List<TestToPageComponentEntry> Components { get; init; } = [];

    public List<string> IgnoredRazor { get; init; } = [];

    /// <summary>Reads <see cref="RelativePath"/> from <paramref name="checkout"/>.</summary>
    public static TestToPageMap Load(RepositoryCheckout checkout)
    {
        var map = JsonSerializer.Deserialize<TestToPageMap>(
            checkout.ReadText(RelativePath),
            SerializerOptions());
        if (map is null || map.Tests.Count == 0)
        {
            throw new InvalidOperationException($"{RelativePath} has no tests.");
        }

        return map;
    }

    /// <summary>
    /// Fixture class names for <paramref name="razorPaths"/>. Components expand to host
    /// pages; <c>.razor.cs</c> matches its <c>.razor</c>; ignored paths select nothing.
    /// </summary>
    public IReadOnlyList<string> TestsForRazorPaths(IEnumerable<string> razorPaths)
    {
        var wanted = ExpandToPagePaths(razorPaths);
        if (wanted.Count == 0)
        {
            return [];
        }

        return Tests
            .Where(entry => entry.Pages.Select(CanonicalRazorPath).Any(wanted.Contains))
            .Select(entry => entry.Class)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Checkout-relative page paths selected by <paramref name="razorPaths"/> after
    /// component expansion (hosts recurse through other <see cref="Components"/> entries).
    /// Empty when every path is ignored or blank.
    /// </summary>
    public HashSet<string> ExpandToPagePaths(IEnumerable<string> razorPaths)
    {
        var ignored = IgnoredRazor.Select(CanonicalRazorPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hosts = ComponentHosts();
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in razorPaths.Select(CanonicalRazorPath).Where(static path => path.Length > 0))
        {
            if (ignored.Contains(raw))
            {
                continue;
            }

            CollectHosts(raw, hosts, wanted, []);
        }

        return wanted;
    }

    private static void CollectHosts(
        string path,
        IReadOnlyDictionary<string, string[]> hosts,
        HashSet<string> wanted,
        HashSet<string> visiting)
    {
        if (!visiting.Add(path))
        {
            return;
        }

        if (!hosts.TryGetValue(path, out var next))
        {
            wanted.Add(path);
            return;
        }

        foreach (var host in next)
        {
            CollectHosts(host, hosts, wanted, visiting);
        }
    }

    /// <summary>Checkout-relative <c>.razor</c> path; strips a trailing <c>.cs</c> from code-behind.</summary>
    public static string CanonicalRazorPath(string path)
    {
        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase))
        {
            return normalized[..^3];
        }

        return normalized;
    }

    private Dictionary<string, string[]> ComponentHosts()
    {
        return Components.ToDictionary(
            entry => CanonicalRazorPath(entry.Path),
            entry => entry.Pages.Select(CanonicalRazorPath).ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions SerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
    }
}

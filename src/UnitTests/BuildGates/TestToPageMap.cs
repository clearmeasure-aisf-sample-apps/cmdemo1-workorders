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

/// <summary>
/// Checked-in map from page-level acceptance tests to Razor pages. A changed Razor
/// file selects its mapped fixtures for changed-UI videos (issue #1670 / item 16).
/// </summary>
internal sealed class TestToPageMap
{
    public const string RelativePath = "src/AcceptanceTests/test-to-page-map.json";

    public List<TestToPageMapEntry> Tests { get; init; } = [];

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
    /// Fixture class names whose <c>pages</c> include any of <paramref name="razorPaths"/>
    /// (checkout-relative). A <c>.razor.cs</c> path matches its <c>.razor</c> page.
    /// </summary>
    public IReadOnlyList<string> TestsForRazorPaths(IEnumerable<string> razorPaths)
    {
        var wanted = new HashSet<string>(
            razorPaths.Select(CanonicalRazorPath).Where(static path => path.Length > 0),
            StringComparer.OrdinalIgnoreCase);
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

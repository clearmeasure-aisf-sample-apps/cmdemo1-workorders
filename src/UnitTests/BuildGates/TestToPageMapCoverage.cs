namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Problems when the test-to-page map drifts from page-level acceptance fixtures or Razor paths.
/// </summary>
internal static class TestToPageMapCoverage
{
    /// <summary>
    /// Missing mappings, empty pages, unknown fixtures, duplicate classes, and missing Razor files.
    /// </summary>
    public static IReadOnlyList<string> Evaluate(
        TestToPageMap map,
        IReadOnlyList<DiscoveredAcceptanceClass> pageLevel,
        RepositoryCheckout checkout)
    {
        var problems = new List<string>();
        problems.AddRange(MissingMappings(map, pageLevel));
        problems.AddRange(UnknownMappings(map, pageLevel));
        problems.AddRange(DuplicateClasses(map));
        problems.AddRange(EmptyOrBlankEntries(map));
        problems.AddRange(MissingRazorFiles(map, checkout));
        return problems;
    }

    private static IEnumerable<string> MissingMappings(
        TestToPageMap map,
        IReadOnlyList<DiscoveredAcceptanceClass> pageLevel)
    {
        var mapped = map.Tests.Select(entry => entry.Class).ToHashSet(StringComparer.Ordinal);
        foreach (var fixture in pageLevel)
        {
            if (!mapped.Contains(fixture.Name))
            {
                yield return
                    $"{fixture.Name} is page-level but missing from {TestToPageMap.RelativePath}.";
            }
        }
    }

    private static IEnumerable<string> UnknownMappings(
        TestToPageMap map,
        IReadOnlyList<DiscoveredAcceptanceClass> pageLevel)
    {
        var known = pageLevel.Select(fixture => fixture.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in map.Tests)
        {
            if (!known.Contains(entry.Class))
            {
                yield return
                    $"{entry.Class} is in {TestToPageMap.RelativePath} but is not a page-level acceptance fixture.";
            }
        }
    }

    private static IEnumerable<string> DuplicateClasses(TestToPageMap map)
    {
        foreach (var group in map.Tests.GroupBy(entry => entry.Class, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            yield return $"{group.Key} appears more than once in {TestToPageMap.RelativePath}.";
        }
    }

    private static IEnumerable<string> EmptyOrBlankEntries(TestToPageMap map)
    {
        foreach (var entry in map.Tests)
        {
            if (string.IsNullOrWhiteSpace(entry.Class))
            {
                yield return $"A map entry in {TestToPageMap.RelativePath} has a blank class name.";
            }

            if (entry.Pages.Count == 0 || entry.Pages.Any(string.IsNullOrWhiteSpace))
            {
                yield return
                    $"{entry.Class} in {TestToPageMap.RelativePath} must list at least one Razor page path.";
            }
        }
    }

    private static IEnumerable<string> MissingRazorFiles(TestToPageMap map, RepositoryCheckout checkout)
    {
        foreach (var path in map.Tests.SelectMany(entry => entry.Pages).Distinct(StringComparer.Ordinal))
        {
            var canonical = TestToPageMap.CanonicalRazorPath(path);
            if (!File.Exists(checkout.PathOf(canonical)))
            {
                yield return
                    $"{canonical} is listed in {TestToPageMap.RelativePath} but was not found in the checkout.";
            }
        }
    }
}

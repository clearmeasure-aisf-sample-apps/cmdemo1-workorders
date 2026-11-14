namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Problems when the test-to-page map drifts from page-level acceptance fixtures or Razor paths.
/// </summary>
internal static class TestToPageMapCoverage
{
    /// <summary>
    /// Missing mappings, empty pages, unknown fixtures, duplicate classes, missing Razor files,
    /// Components/*.razor files without a host-page entry, and component hosts that do not match
    /// scanned markup usage (<see cref="RazorComponentHostScanner"/>). Layout/shell
    /// expansion to routable pages is enforced by BuildGates selection tests that load
    /// the map through <see cref="TestToPageMap.Load"/> (<see cref="RazorLayoutPageScanner"/>).
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
        problems.AddRange(ComponentEntryProblems(map));
        problems.AddRange(ComponentHostUsageMismatches(map, checkout));
        problems.AddRange(UnmappedUiRazor(map, checkout));
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
        var listed = map.Tests.SelectMany(entry => entry.Pages)
            .Concat(map.Components.Select(entry => entry.Path))
            .Concat(map.Components.SelectMany(entry => entry.Pages))
            .Concat(map.IgnoredRazor)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var path in listed)
        {
            var canonical = TestToPageMap.CanonicalRazorPath(path);
            if (!File.Exists(checkout.PathOf(canonical)))
            {
                yield return
                    $"{canonical} is listed in {TestToPageMap.RelativePath} but was not found in the checkout.";
            }
        }
    }

    private static IEnumerable<string> ComponentEntryProblems(TestToPageMap map)
    {
        foreach (var group in map.Components.GroupBy(entry => TestToPageMap.CanonicalRazorPath(entry.Path),
                     StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            yield return $"{group.Key} appears more than once under components in {TestToPageMap.RelativePath}.";
        }

        foreach (var entry in map.Components)
        {
            if (string.IsNullOrWhiteSpace(entry.Path) || entry.Pages.Count == 0 ||
                entry.Pages.Any(string.IsNullOrWhiteSpace))
            {
                yield return
                    $"A components entry in {TestToPageMap.RelativePath} needs a path and at least one host page.";
            }
        }
    }


    private static IEnumerable<string> ComponentHostUsageMismatches(
        TestToPageMap map,
        RepositoryCheckout checkout)
    {
        var scanned = RazorComponentHostScanner.Scan(checkout);
        var mapped = map.Components.ToDictionary(
            entry => TestToPageMap.CanonicalRazorPath(entry.Path),
            entry => entry.Pages.Select(TestToPageMap.CanonicalRazorPath)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var (component, hosts) in scanned)
        {
            if (!mapped.TryGetValue(component, out var listed))
            {
                yield return
                    $"{component} is used as a shared component but has no components[] entry in {TestToPageMap.RelativePath}.";
                continue;
            }

            if (!listed.SequenceEqual(hosts, StringComparer.OrdinalIgnoreCase))
            {
                yield return
                    $"{component} components[] hosts [{string.Join(", ", listed)}] do not match scanned usage hosts [{string.Join(", ", hosts)}].";
            }
        }
    }

    private static IEnumerable<string> UnmappedUiRazor(TestToPageMap map, RepositoryCheckout checkout)
    {
        var covered = map.Tests.SelectMany(entry => entry.Pages)
            .Concat(map.Components.Select(entry => entry.Path))
            .Concat(map.IgnoredRazor)
            .Select(TestToPageMap.CanonicalRazorPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in checkout.SourceFiles(".razor"))
        {
            var relative = checkout.Relative(path);
            if (!IsUiRazor(relative) || covered.Contains(relative))
            {
                continue;
            }

            yield return
                $"{relative} is a UI Razor file but is not listed under tests, components, or ignoredRazor in {TestToPageMap.RelativePath}.";
        }
    }

    private static bool IsUiRazor(string relative)
    {
        return relative.StartsWith("src/UI.Shared/", StringComparison.Ordinal)
               || relative.StartsWith("src/UI/Client/", StringComparison.Ordinal);
    }
}

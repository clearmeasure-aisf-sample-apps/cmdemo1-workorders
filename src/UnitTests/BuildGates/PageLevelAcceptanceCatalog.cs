using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Acceptance fixtures that exercise the browser UI (page-level). API-only, MCP-without-browser,
/// Extensions, and TestUsers fixtures are excluded. New page-level fixtures must appear in
/// <c>test-to-page-map.json</c>.
/// </summary>
internal static class PageLevelAcceptanceCatalog
{
    private static readonly HashSet<string> AlwaysPageLevelBases = new(StringComparer.Ordinal)
    {
        "AppAcceptanceTestBase",
        "AuthAcceptanceTestBase",
        "WorkOrderAcceptanceTestBase",
        "BuildingsAcceptanceTestBase",
        "TeamsAndVendorsAcceptanceTestBase",
        "AiAgentsAcceptanceTestBase",
    };

    private static readonly HashSet<string> BrowserUnlessDisabledBases = new(StringComparer.Ordinal)
    {
        "McpAcceptanceTestBase",
        "AcceptanceTestBase",
        "AcceptanceApiTestBase",
    };

    private static readonly Regex NamespaceLine = new(
        @"^namespace\s+(?<name>[\w.]+)\s*;",
        RegexOptions.Multiline);

    private static readonly Regex PublicClassWithBase = new(
        @"^public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(?<name>\w+)\s*:\s*(?<base>\w+)",
        RegexOptions.Multiline);

    private static readonly Regex PublicClass = new(
        @"^public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(?<name>\w+)",
        RegexOptions.Multiline);

    private static readonly Regex TestAttribute = new(@"^\s*\[Test\b", RegexOptions.Multiline);

    private static readonly Regex TestCaseAttribute = new(@"^\s*\[TestCase\b(?!Source)", RegexOptions.Multiline);

    private static readonly Regex TestCaseSourceAttribute = new(@"^\s*\[TestCaseSource\b", RegexOptions.Multiline);

    private static readonly Regex YieldReturn = new(@"^\s*yield return\b", RegexOptions.Multiline);

    /// <summary>Page-level acceptance fixtures under <c>src/AcceptanceTests</c>, in name order.</summary>
    public static IReadOnlyList<DiscoveredAcceptanceClass> Read(RepositoryCheckout checkout)
    {
        var found = new List<DiscoveredAcceptanceClass>();
        foreach (var path in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(path);
            if (!relative.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            if (!IsPageLevel(text) || CountTests(text) == 0)
            {
                continue;
            }

            found.Add(new DiscoveredAcceptanceClass(TypeName(relative, text), CountTests(text)));
        }

        found.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));
        return found;
    }

    /// <summary>True when the fixture source is a browser UI (page-level) acceptance test.</summary>
    private static bool IsPageLevel(string text)
    {
        if (text.Contains("RequiresBrowser => false", StringComparison.Ordinal))
        {
            return false;
        }

        var bases = PublicClassWithBase.Matches(text);
        if (bases.Count != 1)
        {
            return false;
        }

        var baseName = bases[0].Groups["base"].Value;
        if (AlwaysPageLevelBases.Contains(baseName))
        {
            return true;
        }

        return BrowserUnlessDisabledBases.Contains(baseName);
    }

    private static int CountTests(string text)
    {
        var yielded = TestCaseSourceAttribute.IsMatch(text) ? YieldReturn.Matches(text).Count : 0;
        if (yielded > 0)
        {
            return yielded;
        }

        return TestAttribute.Matches(text).Count + TestCaseAttribute.Matches(text).Count;
    }

    private static string TypeName(string relativePath, string text)
    {
        var ns = NamespaceLine.Match(text);
        var types = PublicClass.Matches(text).Select(match => match.Groups["name"].Value).ToArray();
        if (!ns.Success || types.Length != 1)
        {
            throw new InvalidOperationException(
                $"{relativePath} has acceptance tests but not exactly one file-scoped namespace and public class.");
        }

        return $"{ns.Groups["name"].Value}.{types[0]}";
    }
}

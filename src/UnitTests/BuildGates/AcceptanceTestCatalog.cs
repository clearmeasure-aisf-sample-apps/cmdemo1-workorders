using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Lists acceptance fixtures from source so a new class cannot skip the shard ledger.
/// </summary>
internal static class AcceptanceTestCatalog
{
    private static readonly Regex TestAttribute = new(@"^\s*\[Test\b", RegexOptions.Multiline);

    /// <summary>Bare <c>[TestCase(...)]</c> only — not <c>[TestCaseSource]</c>.</summary>
    private static readonly Regex TestCaseAttribute = new(@"^\s*\[TestCase\b(?!Source)", RegexOptions.Multiline);

    private static readonly Regex TestCaseSourceAttribute = new(@"^\s*\[TestCaseSource\b", RegexOptions.Multiline);

    private static readonly Regex YieldReturn = new(@"^\s*yield return\b", RegexOptions.Multiline);

    private static readonly Regex NamespaceLine = new(
        @"^namespace\s+(?<name>[\w.]+)\s*;",
        RegexOptions.Multiline);

    private static readonly Regex PublicClass = new(
        @"^public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(?<name>\w+)",
        RegexOptions.Multiline);

    /// <summary>Every acceptance fixture under <c>src/AcceptanceTests</c>, in name order.</summary>
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

            var discovered = ReadFile(relative, File.ReadAllText(path));
            if (discovered is not null)
            {
                found.Add(discovered);
            }
        }

        found.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));
        return found;
    }

    private static DiscoveredAcceptanceClass? ReadFile(string relativePath, string text)
    {
        var tests = CountTests(text);
        if (tests == 0)
        {
            return null;
        }

        var typeName = TypeName(relativePath, text);
        return new DiscoveredAcceptanceClass(typeName, tests);
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

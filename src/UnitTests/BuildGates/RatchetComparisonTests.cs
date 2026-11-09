using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.BuildGates.RatchetKeysMother;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class RatchetComparisonTests
{
    [Test]
    public void ShouldNameTheRatchetAndEveryList_WhenReported()
    {
        var comparison = new RatchetComparison([LongMethod], [WideFile], [WideParameters]);

        var report = comparison.Report("Size and nesting");

        ShouldContainInOrder(
            report,
            "Size and nesting ratchet mismatch. Unexpected 1, stale 1, repeated 1.",
            "Unexpected (new violations) (1):",
            LongMethod,
            "Stale (delete these baseline lines) (1):",
            WideFile,
            "Repeated (keep one of each) (1):",
            WideParameters);
    }

    [Test]
    public void ShouldListTwentyKeysAndCountTheRest_WhenAListIsLonger()
    {
        var keys = Numbered(23);
        var comparison = new RatchetComparison(keys, [], []);

        var report = comparison.Report("Sample");

        ShouldShowFirstTwentyOf(report, keys);
        report.ShouldContain("... 3 more");
    }

    private static void ShouldContainInOrder(string report, params string[] fragments)
    {
        var position = -1;
        foreach (var fragment in fragments)
        {
            var next = report.IndexOf(fragment, position + 1, StringComparison.Ordinal);
            next.ShouldBeGreaterThan(position, $"'{fragment}' is missing or out of order in:\n{report}");
            position = next;
        }
    }

    private static void ShouldShowFirstTwentyOf(string report, string[] keys)
    {
        keys.Take(20).ShouldAllBe(key => report.Contains(key));
        keys.Skip(20).ShouldAllBe(key => !report.Contains(key));
    }
}

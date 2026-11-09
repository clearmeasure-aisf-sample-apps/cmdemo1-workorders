using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.BuildGates.RatchetKeysMother;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class KeyRatchetTests
{
    [Test]
    public void ShouldReportUnexpected_WhenScanFindsAKeyTheBaselineDoesNotList()
    {
        var ratchet = Listing(WideFile);

        var comparison = ratchet.Compare([WideFile, LongMethod]);

        ShouldDifferOnlyBy(comparison, unexpected: [LongMethod], stale: []);
    }

    [Test]
    public void ShouldReportStale_WhenBaselineListsAKeyTheScanNoLongerFinds()
    {
        var ratchet = Listing(WideFile, LongMethod);

        var comparison = ratchet.Compare([WideFile]);

        ShouldDifferOnlyBy(comparison, unexpected: [], stale: [LongMethod]);
    }

    [Test]
    public void ShouldBeClean_WhenScanListsTheBaselineKeysInAnotherOrder()
    {
        var ratchet = Listing(LongMethod, WideFile);

        var comparison = ratchet.Compare([WideFile, LongMethod]);

        comparison.IsClean.ShouldBeTrue(comparison.Report("Sample"));
    }

    [Test]
    public void ShouldReportRepeated_WhenBaselineListsAKeyTwice()
    {
        var ratchet = Listing(WideFile, LongMethod, WideFile);

        var comparison = ratchet.Compare([WideFile, LongMethod]);

        ShouldOnlyRepeat(comparison, WideFile);
    }

    [Test]
    public void ShouldKeepOnlyKeys_WhenBaselineHasCommentsBlankLinesAndAByteOrderMark()
    {
        var ratchet = KeyRatchet.Parse(CommentedBaselineText());

        ratchet.Keys.ShouldBe([WideFile, LongMethod]);
    }

    private static void ShouldDifferOnlyBy(RatchetComparison comparison, string[] unexpected, string[] stale)
    {
        comparison.Unexpected.ShouldBe(unexpected);
        comparison.Stale.ShouldBe(stale);
        comparison.Repeated.ShouldBeEmpty();
        comparison.IsClean.ShouldBeFalse();
    }

    private static void ShouldOnlyRepeat(RatchetComparison comparison, string key)
    {
        comparison.Repeated.ShouldBe([key]);
        comparison.Unexpected.ShouldBeEmpty();
        comparison.Stale.ShouldBeEmpty();
        comparison.IsClean.ShouldBeFalse();
    }
}

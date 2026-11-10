using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class SizeAndNestingBaselineTests
{
    private const string BaselinePath = "src/UnitTests/BuildGates/size-nesting-baseline.txt";

    /// <summary>Shown with every new violation: the fix is a refactoring, never a mechanical split.</summary>
    private const string PlaybookGuidance =
        "Each key names the file and the limit it breaks. Read and follow docs/refactoring-oversized-code.md " +
        "before editing: name the smell, apply a preferred move, name the new abstraction. Never split by line count.";

    [Test]
    public void ShouldNameTheFileAndPointToThePlaybook_WhenANewViolationIsReported()
    {
        var comparison = KeyRatchet.Parse("").Compare(["method src/Core/A.cs A.Run()"]);

        var report = comparison.Report("Size and nesting", PlaybookGuidance);

        report.ShouldContain("method src/Core/A.cs A.Run()");
        report.ShouldContain("docs/refactoring-oversized-code.md");
    }

    [Test]
    public void ShouldOmitThePlaybook_WhenOnlyStaleLinesRemain()
    {
        var comparison = KeyRatchet.Parse("file src/Core/A.cs").Compare([]);

        var report = comparison.Report("Size and nesting", PlaybookGuidance);

        report.ShouldNotContain("docs/refactoring-oversized-code.md");
    }

    [Test]
    public void ShouldMatchRatchetBaseline_WhenRepositoryIsScanned()
    {
        var comparison = CompareRepositoryWithBaseline();

        comparison.IsClean.ShouldBeTrue(comparison.Report("Size and nesting", PlaybookGuidance));
    }

    private static RatchetComparison CompareRepositoryWithBaseline()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.ReadRatchet(BaselinePath).Compare(SizeAndNestingScanner.ScanRepository(checkout));
    }
}

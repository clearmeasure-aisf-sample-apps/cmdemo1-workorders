using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class SizeAndNestingBaselineTests
{
    private const string BaselinePath = "src/UnitTests/BuildGates/size-nesting-baseline.txt";

    [Test]
    public void ShouldNameTheFileAndPointToThePlaybook_WhenANewViolationIsFormatted()
    {
        var diff = SizeAndNestingBaseline.Diff(["method src/Core/A.cs A.Run()"], []);

        var message = SizeAndNestingBaseline.Format(diff);

        message.ShouldContain("method src/Core/A.cs A.Run()");
        message.ShouldContain("docs/refactoring-oversized-code.md");
    }

    [Test]
    public void ShouldOmitThePlaybook_WhenOnlyStaleLinesRemain()
    {
        var diff = SizeAndNestingBaseline.Diff([], ["file src/Core/A.cs"]);

        var message = SizeAndNestingBaseline.Format(diff);

        message.ShouldNotContain("docs/refactoring-oversized-code.md");
    }

    [Test]
    public void ShouldMatchRatchetBaseline_WhenRepositoryIsScanned()
    {
        var comparison = CompareRepositoryWithBaseline();

        comparison.IsClean.ShouldBeTrue(comparison.Report("Size and nesting"));
    }

    private static RatchetComparison CompareRepositoryWithBaseline()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.ReadRatchet(BaselinePath).Compare(SizeAndNestingScanner.ScanRepository(checkout));
    }
}

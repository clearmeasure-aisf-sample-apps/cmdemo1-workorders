using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class SizeAndNestingBaselineTests
{
    private const string BaselinePath = "src/UnitTests/BuildGates/size-nesting-baseline.txt";

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

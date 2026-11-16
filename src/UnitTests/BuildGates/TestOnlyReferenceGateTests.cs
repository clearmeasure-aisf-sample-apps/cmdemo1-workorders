using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class TestOnlyReferenceGateTests
{
    private const string Guidance =
        "A production public/internal member or type is referenced only from test projects. " +
        "Delete it and its tests (Remove Dead Code), or use it from production. " +
        "Never add a baseline line for new debt; baselines only shrink.";

    [Test]
    public void ShouldMatchRatchetBaseline_WhenRepositoryIsScanned()
    {
        var comparison = CompareRepositoryWithBaseline();

        comparison.IsClean.ShouldBeTrue(comparison.Report("Test-only reference", Guidance));
    }

    [Test]
    public void ShouldNameTheMember_WhenANewViolationIsReported()
    {
        var comparison = KeyRatchet.Parse("").Compare(["src/Core/Sample.cs :: Sample.OnlyInTests()"]);

        var report = comparison.Report("Test-only reference", Guidance);

        report.ShouldContain("src/Core/Sample.cs :: Sample.OnlyInTests()");
        report.ShouldContain("Remove Dead Code");
    }

    private static RatchetComparison CompareRepositoryWithBaseline()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.ReadRatchet(TestOnlyReferenceScan.BaselinePath)
            .Compare(TestOnlyReferenceScan.ScanRepository(checkout));
    }
}

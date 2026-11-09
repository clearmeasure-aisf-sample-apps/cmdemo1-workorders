using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class RatchetBaselineTests
{
    private static readonly StructureViolation Known = new("src/Core/A.cs", "WorkOrderHelper");
    private static readonly StructureViolation Added = new("src/Core/B.cs", "SearchUtils");

    [Test]
    public void ShouldReportNewViolation_WhenItIsNotOnTheBaseline()
    {
        var baseline = RatchetBaseline.Of("# legacy helpers", "", Known.Key);

        var comparison = baseline.Compare([Known, Added]);

        comparison.NewViolations.ShouldBe([Added.Key]);
        comparison.StaleEntries.ShouldBeEmpty();
    }

    [Test]
    public void ShouldReportStaleEntry_WhenAViolationWasFixed()
    {
        var baseline = RatchetBaseline.Of(Known.Key, Added.Key);

        var comparison = baseline.Compare([Known]);

        comparison.NewViolations.ShouldBeEmpty();
        comparison.StaleEntries.ShouldBe([Added.Key]);
    }

    [Test]
    public void ShouldTreatAMissingFileAsAnEmptyBaseline_WhenARuleHasNoViolations()
    {
        var baseline = RatchetBaseline.Load(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt"));

        var comparison = baseline.Compare([Added]);

        comparison.NewViolations.ShouldBe([Added.Key]);
    }
}

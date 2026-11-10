using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// The line-count table of the Refactoring plan: <c>Test-RefactoringRationale.ps1</c> generates it from the
/// size-baseline diff and fails a pull request whose table is missing or stale.
/// </summary>
[TestFixture]
public class RefactoringPlanLineCountScriptTests
{
    private const string GeneratedRowForTheShrunkFile = "| `src/McpServer/Tools/WorkOrderTools.cs` | 315 | 5 | 1 | 0 |";

    private RefactoringPullRequestSteps _steps = null!;

    [SetUp]
    public void CreateThrowawayRepository() => _steps = new RefactoringPullRequestSteps();

    [TearDown]
    public void DeleteThrowawayRepository() => _steps.Dispose();

    [Test]
    public void ShouldCountLinesAndBaselineEntriesBeforeAndAfter_WhenTheTableIsGenerated()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ShrinkOversizedFileAndDeleteItsBaselineLine();

        var run = _steps.GenerateLineCountTable();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("| File | Lines before | Lines after | Size-gate entries before | Size-gate entries after |");
        run.Output.ShouldContain(GeneratedRowForTheShrunkFile);
    }

    [Test]
    public void ShouldListAChangedFile_WhenItsBaselineLinesStay()
    {
        _steps.CommitBaseWithSizeBaselineListing();
        _steps.ChangeListedFile();

        var run = _steps.GenerateLineCountTable();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain($"| `{RefactoringPullRequestSteps.ListedFile}` | 5 | 6 | 1 | 1 |");
    }

    [Test]
    public void ShouldFailWithTheGeneratedTable_WhenThePlanHasNoLineCounts()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ShrinkOversizedFileAndDeleteItsBaselineLine();
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithoutLineCounts());

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("The Refactoring plan has no '### Line counts' table");
        run.Output.ShouldContain(GeneratedRowForTheShrunkFile);
    }

    [Test]
    public void ShouldFailWithTheGeneratedTable_WhenTheLineCountsAreStale()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ShrinkOversizedFileAndDeleteItsBaselineLine();
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithLineCounts(
            "| File | Lines before | Lines after | Size-gate entries before | Size-gate entries after |\n" +
            "|------|---|---|---|---|\n" +
            "| `src/McpServer/Tools/WorkOrderTools.cs` | 315 | 120 | 1 | 0 |"));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("The '### Line counts' table is stale");
        run.Output.ShouldContain(GeneratedRowForTheShrunkFile);
    }
}

using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// The paste-ready Refactoring plan: <c>Test-RefactoringRationale.ps1 -WritePlanSkeleton</c> prints it before a pull
/// request is opened, and a failed check prints it when the body has no plan, so a first-pass description passes.
/// </summary>
[TestFixture]
public class RefactoringPlanSkeletonScriptTests
{
    private const string GeneratedRow = "| `src/McpServer/Tools/WorkOrderTools.cs` |";

    private RefactoringPullRequestSteps _steps = null!;

    [SetUp]
    public void CreateThrowawayRepository() => _steps = new RefactoringPullRequestSteps();

    [TearDown]
    public void DeleteThrowawayRepository() => _steps.Dispose();

    [Test]
    public void ShouldPrintAPlanNamingTheAddedTypeAndTheLineCounts_WhenASizeBaselinedFileChanges()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");

        var run = _steps.GeneratePlanSkeleton();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("## Refactoring plan");
        run.Output.ShouldContain("### Refactoring rationale");
        run.Output.ShouldContain("| Smell | Refactoring | New abstraction | Why |");
        run.Output.ShouldContain("    class WorkOrderTools");
        run.Output.ShouldContain("    class WorkOrderDraft");
        run.Output.ShouldContain("### Line counts");
        run.Output.ShouldContain(GeneratedRow);
    }

    [Test]
    public void ShouldSayNoPlanIsNeeded_WhenNoSizeBaselinedFileChanges()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ChangeSmallFile();

        var run = _steps.GeneratePlanSkeleton();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("no Refactoring plan required");
        run.Output.ShouldNotContain("## Refactoring plan");
    }

    [Test]
    public void ShouldPrintThePasteReadyPlan_WhenTheCheckFailsForAMissingPlan()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("----- BEGIN REFACTORING PLAN -----");
        run.Output.ShouldContain("    class WorkOrderDraft");
        run.Output.ShouldContain(GeneratedRow);
        run.Output.ShouldContain("----- END REFACTORING PLAN -----");
    }
}

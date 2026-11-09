using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// Runs <c>scripts/refactoring/Test-RefactoringRationale.ps1</c> over throwaway pull requests.
/// </summary>
[TestFixture]
public class RefactoringRationaleScriptTests
{
    private RefactoringPullRequestSteps _steps = null!;

    [SetUp]
    public void CreateThrowawayRepository() => _steps = new RefactoringPullRequestSteps();

    [TearDown]
    public void DeleteThrowawayRepository() => _steps.Dispose();

    [Test]
    public void ShouldPass_WhenEveryMoveIsExplainedAndTheExtractedTypeHasAFixture()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder());

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain($"Size-baselined file(s) changed: {RefactoringPullRequestSteps.OversizedFile}");
    }

    [Test]
    public void ShouldFail_WhenASizeBaselinedFileChangesWithoutAPlan()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("no '## Refactoring plan' section");
        run.Output.ShouldContain("docs/refactoring-oversized-code.md");
    }

    [TestCase(
        "| Big File | Introduce Parameter Object | WorkOrderDraft | The ten create arguments always travel together and are validated together |",
        "Unknown smell 'Big File'")]
    [TestCase(
        "| Long Parameter List | Split File | WorkOrderDraft | The ten create arguments always travel together and are validated together |",
        "Unknown refactoring 'Split File'")]
    [TestCase(
        "| Long Parameter List | Introduce Parameter Object | WorkOrderRequest | The ten create arguments always travel together and are validated together |",
        "New abstraction 'WorkOrderRequest' does not appear")]
    [TestCase(
        "| Long Parameter List | Introduce Parameter Object | WorkOrderDraft | Too many parameters |",
        "The reason has 3 word(s)")]
    [TestCase(
        "| Long Parameter List | Introduce Parameter Object | | The ten create arguments always travel together and are validated together |",
        "needs four filled cells")]
    public void ShouldFail_WhenARationaleRowBreaksThePlaybook(string row, string expectedFailure)
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithRationaleRow(row));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain(expectedFailure);
    }

    [TestCase("Replace Primitive with Object (Value Object)")]
    [TestCase("Replace Primitive with Object")]
    [TestCase("Value Object")]
    public void ShouldAcceptAMove_WhenItIsWrittenAsThePlaybookNamesItOrByAnAlias(string move)
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithRationaleRow(
            $"| Primitive Obsession | {move} | WorkOrderDraft | The draft fields were raw strings validated separately in every tool |"));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
    }

    [Test]
    public void ShouldPass_WhenAnotherMoveIsNamedWithItsReason()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftCreationTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithRationaleRow(
            "| Long Parameter List | Other: Introduce Builder | WorkOrderDraft | Optional arguments make a parameter object awkward, so a builder reads better here |"));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("Other move 'Introduce Builder' accepted");
    }

    [Test]
    public void ShouldFail_WhenAnExtractedTypeHasNoFixture()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder());

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("Extracted type WorkOrderDraft (src/McpServer/Tools/WorkOrderDraft.cs) has no test fixture");
    }

    [Test]
    public void ShouldFail_WhenAGuardrailBaselineGainsALine()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.AddGuardrailBaselineLine();
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("gained 1 line(s); baselines only shrink");
    }

    [Test]
    public void ShouldPass_WhenAPullRequestIntroducesABaselineTheBaseDoesNotHave()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.AddNewGuardrailBaseline();
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
    }

    [Test]
    public void ShouldPassWithoutAPlan_WhenNoSizeBaselinedFileChanges()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ChangeSmallFile();
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("no Refactoring plan required");
    }

    [Test]
    public void ShouldRequireARationale_WhenTheSizeBaselineListsTheChangedFile()
    {
        _steps.CommitBaseWithSizeBaselineListing();
        _steps.ChangeListedFile();
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain($"changes size-baselined file(s) ({RefactoringPullRequestSteps.ListedFile})");
    }

    [Test]
    public void ShouldNameTheCodeBehind_WhenTheBaselineListsARazorCodeBehindFile()
    {
        _steps.CommitBaseWithCodeBehindOnTheSizeBaseline();
        _steps.ChangeListedCodeBehind();
        _steps.DescribeWithoutPlan();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain($"changes size-baselined file(s) ({RefactoringPullRequestSteps.ListedCodeBehind})");
    }

    [Test]
    public void ShouldFail_WhenThePlanHasNoRationaleTable()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithRationaleRow(""));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("no filled-in '### Refactoring rationale' table");
    }
}

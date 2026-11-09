using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// The Before/After mermaid diagrams of the Refactoring plan, as <c>Test-RefactoringRationale.ps1</c> checks them.
/// </summary>
[TestFixture]
public class RefactoringPlanDiagramScriptTests
{
    private RefactoringPullRequestSteps _steps = null!;

    [SetUp]
    public void CreateThrowawayRepository() => _steps = new RefactoringPullRequestSteps();

    [TearDown]
    public void DeleteThrowawayRepository() => _steps.Dispose();

    [Test]
    public void ShouldPass_WhenAFlowchartPairNamesTheExtractedType()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams(
            ("**Before**", "flowchart LR\n    tool[WorkOrderTools] --> bus[IBus]"),
            ("**After**", "flowchart LR\n    tool[WorkOrderTools] --> draft[WorkOrderDraft] --> bus[IBus]")));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
    }

    [Test]
    public void ShouldFail_WhenThePlanHasNoMermaidDiagram()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams());

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("The Refactoring plan has no mermaid diagram");
    }

    [Test]
    public void ShouldFail_WhenAnExtractedTypeIsMissingFromTheDiagrams()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams(
            ("Before", "classDiagram\n    class WorkOrderTools"),
            ("After", "classDiagram\n    class WorkOrderTools")));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain(
            "New type WorkOrderDraft (src/McpServer/Tools/WorkOrderDraft.cs) does not appear in any Refactoring plan diagram");
    }

    [Test]
    public void ShouldFail_WhenADiagramIsNeitherAClassDiagramNorAFlowchart()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams(
            ("Before", "sequenceDiagram\n    WorkOrderTools->>IBus: Send"),
            ("After", "classDiagram\n    WorkOrderTools --> WorkOrderDraft")));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("Mermaid diagram 'Before' is a 'sequenceDiagram'; use classDiagram or flowchart");
    }

    [Test]
    public void ShouldFail_WhenADiagramIsNotLabelledBeforeOrAfter()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams(
            ("Before", "classDiagram\n    class WorkOrderTools"),
            ("The new shape", "classDiagram\n    WorkOrderTools --> WorkOrderDraft")));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("Mermaid diagram 'The new shape' is not labelled");
    }

    [Test]
    public void ShouldFail_WhenBeforeAndAfterDiagramsDoNotPairUp()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithPlan(new RefactoringPlanBuilder().WithDiagrams(
            ("After", "classDiagram\n    WorkOrderTools --> WorkOrderDraft")));

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("The Refactoring plan has 0 Before and 1 After diagram(s)");
    }
}

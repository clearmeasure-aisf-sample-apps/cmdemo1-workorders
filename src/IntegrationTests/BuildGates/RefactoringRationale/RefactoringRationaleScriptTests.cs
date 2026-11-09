using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// Runs <c>scripts/refactoring/Test-RefactoringRationale.ps1</c> over throwaway pull requests.
/// </summary>
[TestFixture]
public class RefactoringRationaleScriptTests
{
    private const string ValidRow =
        "| Long Parameter List, Data Clumps | Introduce Parameter Object | WorkOrderDraft | The ten create arguments always travel together and are validated together |";

    private RefactoringPullRequestSteps _steps = null!;

    [SetUp]
    public void CreateThrowawayRepository() => _steps = new RefactoringPullRequestSteps(FindRepoRoot());

    [TearDown]
    public void DeleteThrowawayRepository() => _steps.Dispose();

    [Test]
    public void ShouldPass_WhenEveryMoveIsExplainedAndTheExtractedTypeHasAFixture()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithRationale(ValidRow);

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain($"Oversized-baseline file(s) changed: {RefactoringPullRequestSteps.OversizedFile}");
    }

    [Test]
    public void ShouldFail_WhenAnOversizedFileChangesWithoutARationale()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftTests");
        _steps.DescribeWithoutRationale();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("no filled-in '## Refactoring rationale' table");
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
        _steps.DescribeWithRationale(row);

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain(expectedFailure);
    }

    [Test]
    public void ShouldPass_WhenAnotherMoveIsNamedWithItsReason()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.AddFixtureFor("WorkOrderDraftCreationTests");
        _steps.DescribeWithRationale(
            "| Long Parameter List | Other: Introduce Builder | WorkOrderDraft | Optional arguments make a parameter object awkward, so a builder reads better here |");

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("Other move 'Introduce Builder' accepted");
    }

    [Test]
    public void ShouldFail_WhenAnExtractedTypeHasNoFixture()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ExtractType("WorkOrderDraft");
        _steps.DescribeWithRationale(ValidRow);

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("Extracted type WorkOrderDraft (src/McpServer/Tools/WorkOrderDraft.cs) has no test fixture");
    }

    [Test]
    public void ShouldFail_WhenAGuardrailBaselineGainsALine()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.AddGuardrailBaselineLine();
        _steps.DescribeWithoutRationale();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain("gained 1 line(s); baselines only shrink");
    }

    [Test]
    public void ShouldPassWithoutARationale_WhenNoOversizedFileChanges()
    {
        _steps.CommitBaseWithOversizedFile();
        _steps.ChangeSmallFile();
        _steps.DescribeWithoutRationale();

        var run = _steps.RunRationaleCheck();

        run.ExitCode.ShouldBe(0, run.Output);
        run.Output.ShouldContain("no Refactoring rationale required");
    }

    [Test]
    public void ShouldRequireARationale_WhenTheSizeBaselineListsTheChangedFile()
    {
        _steps.CommitBaseWithSizeBaselineListing();
        _steps.ChangeListedFile();
        _steps.DescribeWithoutRationale();

        var run = _steps.RunRationaleCheck(RefactoringPullRequestSteps.SizeBaseline);

        run.ExitCode.ShouldBe(1, run.Output);
        run.Output.ShouldContain($"changes oversized-baseline file(s) ({RefactoringPullRequestSteps.ListedFile})");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ChurchBulletin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found from test directory.");
    }
}

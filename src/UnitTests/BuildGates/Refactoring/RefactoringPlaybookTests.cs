using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// The playbook carries the tables the rationale script reads, the pull request template carries the section it
/// checks, and every agent instruction file sends a size-gate failure to the playbook.
/// </summary>
[TestFixture]
public class RefactoringPlaybookTests
{
    private const string Playbook = "docs/refactoring-oversized-code.md";

    [TestCase("### Smells")]
    [TestCase("## Preferred moves")]
    [TestCase("## Banned moves")]
    [TestCase("## Test files")]
    [TestCase("| Smell | Signals in this codebase |")]
    [TestCase("| Refactoring | Fixes | Source | In this codebase |")]
    public void ShouldCarryTheSectionsTheChecksRead_WhenThePlaybookIsRead(string expected)
    {
        var playbook = ReadRepositoryFile(Playbook);

        playbook.ShouldContain(expected);
    }

    [TestCase("Large Class")]
    [TestCase("Divergent Change")]
    [TestCase("Shotgun Surgery")]
    [TestCase("Feature Envy")]
    [TestCase("Data Clumps")]
    [TestCase("Primitive Obsession")]
    [TestCase("Switch Statements")]
    [TestCase("Long Parameter List")]
    [TestCase("Long Method")]
    public void ShouldNameTheSmell_WhenThePlaybookIsRead(string smell)
    {
        var playbook = ReadRepositoryFile(Playbook);

        playbook.ShouldContain($"| {smell} |");
    }

    [Test]
    public void ShouldOfferTheRationaleTable_WhenAPullRequestIsOpened()
    {
        var template = ReadRepositoryFile(".github/pull_request_template.md");

        template.ShouldContain("## Refactoring rationale");
        template.ShouldContain("| Smell | Refactoring | New abstraction | Why |");
        template.ShouldContain(Playbook);
    }

    [TestCase("AGENTS.md")]
    [TestCase("CLAUDE.md")]
    [TestCase(".cursor/rules/refactoring-oversized-code.mdc")]
    public void ShouldSendASizeGateFailureToThePlaybook_WhenAgentInstructionsAreRead(string instructions)
    {
        var text = ReadRepositoryFile(instructions);

        text.ShouldContain("size-gate failure");
        text.ShouldContain(Playbook);
    }

    [Test]
    public void ShouldRunTheRationaleScript_WhenAPullRequestChanges()
    {
        var workflow = ReadRepositoryFile(".github/workflows/refactoring-rationale.yml");

        workflow.ShouldContain("pull_request:");
        workflow.ShouldContain("edited");
        workflow.ShouldContain("scripts/refactoring/Test-RefactoringRationale.ps1");
    }

    private static string ReadRepositoryFile(string relativePath) => RepositoryCheckout.Current.ReadText(relativePath);
}

using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Runs the suppression-budget script. Every disable in src needs a reason, the count
/// cannot rise above the committed baseline, and task-marker comments fail the build.
/// </summary>
[TestFixture]
public sealed class SuppressionBudgetGateTests
{
    [Test]
    public async Task ShouldPass_WhenSelfTestRuns()
    {
        var result = await RunScript("-SelfTest");

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("missing reason fails");
        result.Output.ShouldContain("increase fails");
        result.Output.ShouldContain("decrease passes");
        result.Output.ShouldContain("suppression budget self-test passed.");
    }

    [Test]
    public async Task ShouldPass_WhenRepositoryStaysWithinSuppressionBudget()
    {
        var result = await RunScript();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("suppression budget passed:");
    }

    private static Task<CommandOutcome> RunScript(params string[] arguments)
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/quality/Test-SuppressionBudget.ps1");
        return ExternalCommand.Pwsh(script, arguments).In(checkout.Root).RunAsync();
    }
}

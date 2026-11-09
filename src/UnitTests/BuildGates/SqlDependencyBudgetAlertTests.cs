using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The tdd SQL dependency budget is a script and a KQL query. The self-test is the
/// deterministic check; this fixture runs it so the private build fails when the query,
/// threshold, credential skip, or scheduled-query-rule drift comparison regresses.
/// </summary>
[TestFixture]
public class SqlDependencyBudgetAlertTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/appinsights/Invoke-SqlDependencyBudget.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("SQL dependency budget self-test passed.");
    }
}

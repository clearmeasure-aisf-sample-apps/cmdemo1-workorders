using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The docs/ allowlist is a script. This fixture runs its self-test so the
/// private build fails when a top-level docs entry is outside the list.
/// </summary>
[TestFixture]
public class DocsAllowlistTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/docs/Test-DocsAllowlist.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Docs allowlist self-test passed.");
    }
}

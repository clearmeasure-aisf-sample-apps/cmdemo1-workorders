using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The repo-root allowlist is a script. This fixture runs its self-test so the
/// private build fails when a root entry is outside the list or a stray artifact appears.
/// </summary>
[TestFixture]
public class RepoRootAllowlistTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/repo-root/Test-RepoRoot.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Repo root allowlist self-test passed.");
    }
}

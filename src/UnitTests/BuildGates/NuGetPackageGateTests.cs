using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The NuGet package gate is a script that the "NuGet package gate" pull request check runs.
/// This fixture runs its self-test so the private build fails when the gate stops telling a new
/// package from a version bump, or stops honoring the approvals file.
/// </summary>
[TestFixture]
public class NuGetPackageGateTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/nuget/Test-NewNuGetPackages.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("NuGet package gate self-test passed.");
    }
}

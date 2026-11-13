using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

[TestFixture]
public class OctopusDotNetCliScriptTests
{
    [Test]
    public async Task OctopusDotNetCliSelfTest_WhenHelpersAreExercised_ExitsZero()
    {
        var script = RepositoryCheckout.Current.PathOf("scripts/ci/OctopusDotNetCli.Tests.ps1");

        var result = await ExternalCommand.Pwsh(script).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("octopus dotnet cli self-test passed.");
    }

    [Test]
    public void PackageEverything_WhenSourcingHelper_DoesNotSwallowInstallStderr()
    {
        var buildScript = RepositoryCheckout.Current.ReadText("build.ps1");

        buildScript.ShouldContain("OctopusDotNetCli.ps1");
        buildScript.ShouldContain("Initialize-OctopusDotNetCli");
        buildScript.ShouldNotContain("dotnet tool install --global Octopus.DotNet.Cli 2>$null");
    }

    [Test]
    public void Helper_WhenRead_ForcesLatestMajorRollForwardForNet6Tool()
    {
        var helper = RepositoryCheckout.Current.ReadText("scripts/ci/OctopusDotNetCli.ps1");

        helper.ShouldContain("Set-OctopusDotNetCliRollForward");
        helper.ShouldContain("LatestMajor");
    }
}

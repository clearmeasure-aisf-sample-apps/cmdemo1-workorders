using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class OctopusDotNetCliPackWorkflowTests
{
    private const string InstallStep = "Install Octopus.DotNet.Cli";
    private const string PackStep = "Create NuGet Packages";

    [Test]
    public void BuildLinux_WhenPackaging_UsesLatestMajorOnlyOnOctopusInstallAndPackSteps()
    {
        var linuxJob = WorkflowFile.Read(RepositoryCheckout.Current, "build.yml").Job("build-linux");

        linuxJob.Text.ShouldContain("DOTNET_ROLL_FORWARD: LatestMinor");

        var install = linuxJob.Step(InstallStep);
        install.ShouldContain("DOTNET_ROLL_FORWARD: LatestPatch");
        install.ShouldContain("Initialize-OctopusDotNetCli");
        install.ShouldContain("OctopusDotNetCli.ps1");
        // Install must run on PRs (no master-only gate) so TransactionalAction regressions are caught
        // before pack; packaging remains master/workflow_dispatch-only.
        install.ShouldNotContain("refs/heads/master");

        var pack = linuxJob.Step(PackStep);
        pack.ShouldContain("DOTNET_ROLL_FORWARD: LatestMajor");
        pack.ShouldContain("Package-Everything");
        pack.ShouldContain("refs/heads/master");

        var steps = linuxJob.StepNames().ToList();
        steps.IndexOf(InstallStep).ShouldBeLessThan(steps.IndexOf(PackStep));
    }
}

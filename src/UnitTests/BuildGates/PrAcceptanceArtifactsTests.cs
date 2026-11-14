using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>Gates for #1671 private video/trace upload + sticky PR comment scripts.</summary>
public class PrAcceptanceArtifactsTests
{
    [Test]
    public async Task ScrubScript_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Scrub-PlaywrightTraceSecrets.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Scrub-PlaywrightTraceSecrets.Tests.ps1 passed");
    }

    [Test]
    public async Task StickyCommentScript_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Write-StickyPrArtifactComment.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Write-StickyPrArtifactComment.Tests.ps1 passed");
    }

    [Test]
    public async Task SelectionScript_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Get-PrAcceptanceArtifactSelection.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Get-PrAcceptanceArtifactSelection.Tests.ps1 passed");
    }

    [Test]
    public async Task PublishScript_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Publish-PrAcceptanceArtifacts.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Publish-PrAcceptanceArtifacts.Tests.ps1 passed");
    }

    [Test]
    public void Workflow_WhenPresent_RunsAfterPrEnvironmentAndUsesOidcOnly()
    {
        var checkout = RepositoryCheckout.Current;
        var yaml = checkout.ReadText(".github/workflows/pr-environment-artifacts.yml");

        yaml.ShouldContain("workflow_run:");
        yaml.ShouldContain("PR environment");
        yaml.ShouldContain("azure/login@v3");
        yaml.ShouldContain("AZURE_CLIENT_ID_ACR_PUSH");
        yaml.ShouldContain("POOL_ARTIFACTS_STORAGE_ACCOUNT");
        yaml.ShouldContain("Publish-PrAcceptanceArtifacts.ps1");
        yaml.ShouldContain("-Mode Comment");
        yaml.ShouldContain("id-token: write");
        yaml.ShouldContain("azure/login@v3");
        yaml.ShouldNotContain("actions/upload-artifact");
        yaml.ShouldNotContain("generate-sas");
        yaml.ShouldNotContain("--account-key");
        yaml.ShouldNotContain("${{ secrets.");
    }

    [Test]
    public void PublishScript_WhenSourceMentionsAuth_UsesAuthModeLoginOnly()
    {
        var script = RepositoryCheckout.Current.ReadText("scripts/acceptance/Publish-PrAcceptanceArtifacts.ps1");
        script.ShouldContain("--auth-mode login");
        script.ShouldContain("Blocker");
        script.ShouldNotContain("--account-key");
        script.ShouldNotContain("generate-sas");
    }
}

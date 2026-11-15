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
    public async Task WorkflowSecurity_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Test-PrEnvironmentArtifactsWorkflow.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Test-PrEnvironmentArtifactsWorkflow.ps1 self-test passed");
    }

    [Test]
    public async Task WorkflowSecurity_WhenRepositoryWorkflowChecked_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Test-PrEnvironmentArtifactsWorkflow.ps1");

        var result = await ExternalCommand.Pwsh(script).In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Test-PrEnvironmentArtifactsWorkflow.ps1 passed");
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
        yaml.ShouldContain("persist-credentials: false");
        yaml.ShouldContain("ref: ${{ github.sha }}");
        yaml.ShouldNotContain("ref: ${{ github.event.workflow_run.head_sha");
        yaml.ShouldNotContain("actions/upload-artifact");
        yaml.ShouldNotContain("generate-sas");
        yaml.ShouldNotContain("--account-key");
        yaml.ShouldNotContain("${{ secrets.");
    }

    [Test]
    public void Workflow_WhenResolvingPr_PrefersEventPayloadThenCommitPulls()
    {
        var yaml = ArtifactsWorkflowText();
        var eventPr = yaml.IndexOf("github.event.workflow_run.pull_requests[0].number", StringComparison.Ordinal);
        var commitPulls = yaml.IndexOf("commits/${WR_SHA}/pulls", StringComparison.Ordinal);

        eventPr.ShouldBeGreaterThan(0);
        commitPulls.ShouldBeGreaterThan(eventPr);
        yaml.ShouldNotContain("actions/runs/${WR_ID}/pull-requests");
        yaml.ShouldNotContain("continue-on-error:");
    }

    [Test]
    public void Workflow_WhenNoPrFound_SkipsStickyCommentWithExitZero()
    {
        var resolve = ArtifactsWorkflow().Job("sticky-comment")
            .Step("Resolve PR number and run metadata");

        resolve.ShouldContain("No pull request for run ${WR_ID}; skipping sticky comment.");
        resolve.ShouldContain("echo \"skip=true\" >> \"$GITHUB_OUTPUT\"");
        resolve.ShouldContain("exit 0");
        resolve.ShouldContain("if pr=$(gh api \"repos/${WR_REPO}/commits/${WR_SHA}/pulls\"");
        resolve.ShouldContain("treating as no PR.");
    }

    private static WorkflowFile ArtifactsWorkflow() =>
        WorkflowFile.Read(RepositoryCheckout.Current, "pr-environment-artifacts.yml");

    private static string ArtifactsWorkflowText() => ArtifactsWorkflow().Text;

    [Test]
    public void PublishScript_WhenSourceMentionsAuth_UsesAuthModeLoginOnly()
    {
        var script = RepositoryCheckout.Current.ReadText("scripts/acceptance/Publish-PrAcceptanceArtifacts.ps1");
        script.ShouldContain("--auth-mode login");
        script.ShouldContain("Blocker");
        script.ShouldContain("Assert-TraceZipScrubbed");
        script.ShouldNotContain("--account-key");
        script.ShouldNotContain("generate-sas");
    }
}

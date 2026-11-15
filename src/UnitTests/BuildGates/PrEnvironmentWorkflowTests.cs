using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentWorkflowTests
{
    private static string WorkflowText() =>
        RepositoryCheckout.Current.ReadText(".github/workflows/pr-environment.yml");

    [Test]
    public void Workflow_WhenRead_IsNamedPrEnvironmentCheck()
    {
        var text = WorkflowText();
        text.ShouldContain("name: PR environment");
        text.ShouldContain("group: pr-environment-${{ github.ref }}");
        text.ShouldContain("cancel-in-progress: true");
    }

    [Test]
    public void Workflow_WhenRead_UsesTwelveShardsAndFourWorkers()
    {
        var text = WorkflowText();
        text.ShouldContain("PR_ENVIRONMENT_SHARD_COUNT: 12");
        text.ShouldContain("PR_ENVIRONMENT_WORKERS: 4");
        text.ShouldContain("shard: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]");
        text.ShouldContain("Invoke-PrEnvironmentAcceptanceLane.ps1");
        text.ShouldContain("-Lane Parallel");
        text.ShouldContain("-Lane Serial");
    }

    [Test]
    public void Workflow_WhenRead_TriggersOnPushNotPullRequestMergeRef()
    {
        var text = WorkflowText();
        text.ShouldContain("push:");
        text.ShouldNotContain("pull_request:");
        text.ShouldContain("github.sha");
        text.ShouldContain("publish-image.yml");
    }

    [Test]
    public void Workflow_WhenPoolNotLive_SkipsAllAzureAndOctopusMutations()
    {
        var text = WorkflowText();
        text.ShouldContain("pool_live");
        text.ShouldContain("name: Shadow mode");
        text.ShouldContain("Succeed without live Azure or Octopus");
        text.ShouldContain(
            "if: needs.prepare.outputs.should_run == 'true' && needs.prepare.outputs.pool_live == 'true'");
        text.ShouldContain("POOL_LEASE_STORAGE_ACCOUNT");

        var leaseJobStart = text.IndexOf("lease-and-deploy:", StringComparison.Ordinal);
        leaseJobStart.ShouldBeGreaterThan(0);
        var afterLease = text[leaseJobStart..];
        var nextJob = afterLease.IndexOf("\n  parallel-shards:", StringComparison.Ordinal);
        nextJob.ShouldBeGreaterThan(0);
        var leaseJob = afterLease[..nextJob];

        leaseJob.ShouldContain("environment: release");
        leaseJob.ShouldContain("pool_live == 'true'");
        leaseJob.ShouldContain("OctopusDeploy/login@v2");
        leaseJob.ShouldContain("azure/login@v3");
        leaseJob.ShouldContain("az acr import");
        leaseJob.ShouldContain("push-package-action");
        leaseJob.ShouldContain("create-release-action");
        leaseJob.ShouldContain("deploy-release-action");

        var shadowStart = text.IndexOf("name: Shadow mode", StringComparison.Ordinal);
        shadowStart.ShouldBeGreaterThan(0);
        var shadowSection = text[Math.Max(0, shadowStart - 200)..Math.Min(text.Length, shadowStart + 800)];
        shadowSection.ShouldNotContain("OctopusDeploy/login");
        shadowSection.ShouldNotContain("azure/login");
        shadowSection.ShouldNotContain("az acr import");
    }

    [Test]
    public void Workflow_WhenPrivileged_ChecksOutTrustedDefaultBranchScripts()
    {
        var text = WorkflowText();
        text.ShouldContain("Checkout trusted scripts (default branch)");
        text.ShouldContain("ref: ${{ github.event.repository.default_branch }}");
        text.ShouldContain("path: _trusted");
        text.ShouldContain("${TRUSTED}/.github/scripts/");
        text.ShouldContain("require-artifact churchbulletin-nuget-packages-pr");
        text.ShouldContain("dispatch-if-missing");
    }

    [Test]
    public void Workflow_WhenReleaseSlot_AlwaysReleasesLease()
    {
        var text = WorkflowText();
        text.ShouldContain("Release pool lease");
        text.ShouldContain("if: always() && needs.lease-and-deploy.outputs.slot != ''");
        text.ShouldContain("if: always() && needs.prepare.outputs.pool_live == 'true'");
    }

    [Test]
    public void Workflow_WhenRead_SucceedsDocsOnlyAndKeepsVideoOffClock()
    {
        var text = WorkflowText();
        text.ShouldContain("Docs-only success");
        text.ShouldContain("detect-code-changes.sh");
        text.ShouldContain("#1671");
        text.ShouldContain("off this check's clock");
        text.ShouldContain("channel: Pull requests");
        text.ShouldContain("cmdemo1-system");
    }

    [Test]
    public void LeaseScript_WhenRead_ShowsWaitingForASlot()
    {
        var lease = RepositoryCheckout.Current.ReadText("scripts/pool/Acquire-PrEnvironmentLease.ps1");
        lease.ShouldContain("Get-PoolWaitingForSlotMessage");
        lease.ShouldContain("WaitNotifyMinutes");
        lease.ShouldContain("PreferWarmCount");
    }

    [Test]
    public void SupportingScripts_WhenPresent_AreReadable()
    {
        var root = RepositoryCheckout.Current;
        root.ReadText(".github/scripts/wait-for-workflow-run.sh").ShouldContain("require-artifact");
        root.ReadText(".github/scripts/parse-pr-package-version.sh").ShouldContain("2.4");
        root.ReadText("scripts/pool/Release-PrEnvironmentLease.ps1").ShouldContain("release");
        root.ReadText("docs/ops/pr-environment.md").ShouldContain("shadow mode");
    }
}

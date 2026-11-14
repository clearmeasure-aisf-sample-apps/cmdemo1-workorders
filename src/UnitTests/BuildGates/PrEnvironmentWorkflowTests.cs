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
    public void Workflow_WhenRead_SucceedsDocsOnlyAndLeasesViaSystemScripts()
    {
        var text = WorkflowText();
        text.ShouldContain("Docs-only success");
        text.ShouldContain("detect-code-changes.sh");
        text.ShouldContain("Acquire-PrEnvironmentLease.ps1");
        text.ShouldContain("Release-PrEnvironmentLease.ps1");
        text.ShouldContain("post-pr-slot-wait-comment.sh");
        text.ShouldContain("cancel-superseded-octopus-tasks.sh");
        text.ShouldContain("cancel-octopus-server-task.sh");
        text.ShouldContain("channel: Pull requests");
        text.ShouldContain("cmdemo1-system");
    }

    [Test]
    public void Workflow_WhenRead_KeepsVideoUploadOffTheCheckClock()
    {
        var text = WorkflowText();
        text.ShouldContain("#1671");
        text.ShouldContain("off this check's clock");
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
        root.ReadText(".github/scripts/wait-for-workflow-run.sh").ShouldContain("wait-for-workflow-run");
        root.ReadText(".github/scripts/parse-pr-package-version.sh").ShouldContain("2.4");
        root.ReadText("scripts/pool/Release-PrEnvironmentLease.ps1").ShouldContain("release");
        root.ReadText("docs/ops/pr-environment.md").ShouldContain("PR environment");
    }
}

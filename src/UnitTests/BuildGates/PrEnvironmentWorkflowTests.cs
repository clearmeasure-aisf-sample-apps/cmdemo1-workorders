using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentWorkflowTests
{
    private static WorkflowFile Workflow() =>
        WorkflowFile.Read(RepositoryCheckout.Current, "pr-environment.yml");

    private static string WorkflowText() => Workflow().Text;

    [Test]
    public void Workflow_WhenRead_IsNamedPrEnvironmentCheck()
    {
        var text = WorkflowText();
        text.ShouldContain("name: PR environment");
        text.ShouldContain("group: pr-environment-${{ github.ref }}");
        text.ShouldContain("cancel-in-progress: true");
        text.ShouldContain("permissions: {}");
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
    public void Workflow_WhenPoolVarsUnset_SkipsLeaseAndDeployJob()
    {
        var text = WorkflowText();
        text.ShouldContain("name: Shadow mode");
        text.ShouldContain("POOL_RESOURCE_GROUP");
        text.ShouldContain(
            "if: needs.prepare.outputs.should_run == 'true' && needs.prepare.outputs.pool_live == 'true'");
        Workflow().Job("shadow").Text.ShouldNotContain("OctopusDeploy/login");
        Workflow().Job("shadow").Text.ShouldNotContain("azure/login");
    }

    [Test]
    public void LeaseAndDeploy_WhenDryRun_GatesEveryAzureAndOctopusMutation()
    {
        var lease = Workflow().Job("lease-and-deploy");
        lease.Text.ShouldContain("Gate Azure and Octopus mutations on non-DryRun lease");
        lease.Text.ShouldContain("dry_run");
        foreach (var step in MutationStepNames())
        {
            lease.Step(step).ShouldContain("steps.mutate.outputs.allow == 'true'");
        }
    }

    [Test]
    public void PrivilegedJobs_WhenCheckout_UseTrustedRefWithPersistCredentialsFalse()
    {
        foreach (var jobId in new[] { "lease-and-deploy", "release-slot" })
        {
            var job = Workflow().Job(jobId);
            job.Text.ShouldContain("ref: ${{ github.event.repository.default_branch }}");
            job.Text.ShouldContain("path: _trusted");
            job.Text.ShouldContain("persist-credentials: false");
            job.Text.ShouldNotContain("bash .github/scripts/");
            job.Step("Checkout pool manager (cmdemo1-system)").ShouldContain("ref: main");
            job.Step("Checkout pool manager (cmdemo1-system)").ShouldNotContain("ref: master");
        }
    }

    [Test]
    public void Workflow_WhenReleaseSlot_AlwaysReleasesLeaseSafely()
    {
        var release = Workflow().Job("release-slot");
        release.Text.ShouldContain("if: always() && needs.prepare.outputs.pool_live == 'true'");
        release.Step("Release pool lease").ShouldContain("if: always()");
        release.Step("Release pool lease").ShouldContain("No slot leased");
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
        text.ShouldContain("require-artifact churchbulletin-nuget-packages-pr");
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
        root.ReadText("scripts/pool/Release-PrEnvironmentLease.ps1").ShouldContain("No slot leased");
        root.ReadText("docs/ops/pr-environment.md").ShouldContain("shadow mode");
    }

    private static IEnumerable<string> MutationStepNames()
    {
        yield return "Sign in to Octopus";
        yield return "Cancel superseded Octopus tasks for this PR";
        yield return "Wait for Publish image with packages on this SHA";
        yield return "Download publish-only packages";
        yield return "Parse package version";
        yield return "Sign in to Azure for ACR";
        yield return "Retag publish image as the Octopus release number";
        yield return "Push Database package to Octopus";
        yield return "Create Pull requests channel release";
        yield return "Deploy release to pool slot";
        yield return "Record Octopus server task id";
        yield return "Resolve slot application URL";
    }
}

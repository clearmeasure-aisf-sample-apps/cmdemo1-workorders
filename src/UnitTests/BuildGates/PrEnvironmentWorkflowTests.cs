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
    public void LeaseAndDeploy_WhenRead_DeclaresDryRunMutationGate()
    {
        var lease = Workflow().Job("lease-and-deploy");
        lease.Text.ShouldContain("Gate Azure and Octopus mutations on non-DryRun lease");
        lease.Text.ShouldContain("dry_run");
    }

    [TestCase("Sign in to Octopus")]
    [TestCase("Cancel superseded Octopus tasks for this PR")]
    [TestCase("Wait for Publish image with packages on this SHA")]
    [TestCase("Download publish-only packages")]
    [TestCase("Parse package version")]
    [TestCase("Sign in to Azure for ACR")]
    [TestCase("Retag publish image as the Octopus release number")]
    [TestCase("Push Database package to Octopus")]
    [TestCase("Create Pull requests channel release")]
    [TestCase("Deploy release to pool slot")]
    [TestCase("Record Octopus server task id")]
    [TestCase("Resolve slot application URL")]
    public void LeaseAndDeploy_WhenDryRun_GatesMutationStep(string stepName)
    {
        Workflow().Job("lease-and-deploy").Step(stepName)
            .ShouldContain("steps.mutate.outputs.allow == 'true'");
    }

    [TestCase("lease-and-deploy")]
    [TestCase("release-slot")]
    public void PrivilegedJob_WhenCheckout_UsesTrustedRefWithPersistCredentialsFalse(string jobId)
    {
        var job = Workflow().Job(jobId);
        job.Text.ShouldContain("ref: ${{ github.event.repository.default_branch }}");
        job.Text.ShouldContain("path: _trusted");
        job.Text.ShouldContain("persist-credentials: false");
        job.Text.ShouldNotContain("bash .github/scripts/");
        job.Step("Checkout pool manager (cmdemo1-system)").ShouldContain("ref: main");
        job.Step("Checkout pool manager (cmdemo1-system)").ShouldNotContain("ref: master");
    }

    [TestCase("mint-artifact-wrap")]
    [TestCase("upload-acceptance-artifacts")]
    public void ArtifactOidcJob_WhenCheckout_UsesTrustedDefaultBranchOnly(string jobId)
    {
        var job = Workflow().Job(jobId);
        job.Text.ShouldContain("environment: release");
        job.Text.ShouldContain("id-token: write");
        job.Text.ShouldContain("ref: ${{ github.event.repository.default_branch }}");
        job.Text.ShouldContain("persist-credentials: false");
        job.Text.ShouldContain("AZURE_CLIENT_ID_ACR_PUSH");
        job.Text.ShouldContain("azure/login@v3");
        job.Text.ShouldNotContain("${{ secrets.");
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
    public void Workflow_WhenRead_SucceedsDocsOnlyAndKeepsStickyCommentOffClock()
    {
        var text = WorkflowText();
        text.ShouldContain("Docs-only success");
        text.ShouldContain("detect-code-changes.sh");
        text.ShouldContain("#1671");
        text.ShouldContain("off this check's clock");
        text.ShouldContain("Sticky PR artifact comment");
        text.ShouldContain("channel: Pull requests");
        text.ShouldContain("require-artifact churchbulletin-nuget-packages-pr");
    }

    [TestCase("parallel-shards")]
    [TestCase("serial-lane")]
    public void ShardJob_WhenPresent_HasNoOidcOrAzureLogin(string jobId)
    {
        var job = Workflow().Job(jobId);
        job.Text.ShouldNotContain("id-token: write");
        job.Text.ShouldNotContain("azure/login");
        job.Text.ShouldNotContain("environment: release");
    }

    [TestCase("parallel-shards")]
    [TestCase("serial-lane")]
    public void ShardJob_WhenCiphertextHandOff_GatesOnStorageVariable(string jobId)
    {
        var gate = Workflow().Job(jobId).Step("Gate ciphertext hand-off");
        gate.ShouldContain("if: always()");
        gate.ShouldContain("vars.POOL_ARTIFACTS_STORAGE_ACCOUNT");
        gate.ShouldContain("::warning::");
        gate.ShouldContain("ready=false");
        gate.ShouldContain("ready=true");
    }

    [TestCase("parallel-shards")]
    [TestCase("serial-lane")]
    public void ShardJob_WhenCiphertextHandOff_UploadsOnlyEncryptedBundle(string jobId)
    {
        var job = Workflow().Job(jobId);
        job.Step("Encrypt acceptance videos and traces").ShouldContain("-Mode Protect");
        job.Step("Upload ciphertext acceptance bundle").ShouldContain("pr-acceptance-cipher-");
        job.Step("Upload ciphertext acceptance bundle").ShouldContain("retention-days: 1");
        job.Text.ShouldNotContain("generate-sas");
        job.Text.ShouldNotContain("--account-key");
    }

    [TestCase("parallel-shards")]
    [TestCase("serial-lane")]
    public void ShardJob_WhenRecordingVideo_GatesOnStorageVariable(string jobId)
    {
        var job = Workflow().Job(jobId);
        job.Text.ShouldContain("ACCEPTANCE_RECORD_VIDEO");
        job.Text.ShouldContain("vars.POOL_ARTIFACTS_STORAGE_ACCOUNT != '' && 'true' || 'false'");
    }

    [Test]
    public void UploadJob_WhenPresent_UsesTrustedUnwrapAndForbidsSecrets()
    {
        var job = Workflow().Job("upload-acceptance-artifacts");
        job.Step("Unprotect and upload to private storage")
            .ShouldContain("Invoke-PrAcceptanceArtifactUploadFromWraps.ps1");
        job.Step("Unprotect and upload to private storage").ShouldContain("AZURE_STORAGE_KEY");
        job.Step("Unprotect and upload to private storage").ShouldContain("Blocker");
        job.Text.ShouldNotContain("generate-sas");
        job.Text.ShouldNotContain("--account-key");
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
}

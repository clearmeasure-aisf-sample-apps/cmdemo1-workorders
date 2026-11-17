using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The published Native AOT apps must have no trim or AOT warning on the feature-flag path: under AOT a reflection-based
/// serializer call there made every flag read its default (PR #1911). The AOT gate workflow publishes both apps and runs
/// scripts/aot/Assert-NoAotWarnings.ps1 on each publish log; these tests keep the script and that wiring in place.
/// </summary>
[TestFixture]
public class AotFeatureFlagWarningGateTests
{
    private const string Script = "scripts/aot/Assert-NoAotWarnings.ps1";
    private const string CheckStep = "Check the feature-flag path has no AOT warnings";

    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;

        var result = await ExternalCommand.Pwsh(checkout.PathOf(Script), "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("AOT warning-free self-test passed.");
    }

    [TestCase("mcpserver-aot", "Publish McpServer as Native AOT")]
    [TestCase("uiserver-aot", "Publish UI.Server as Native AOT")]
    public void AotGate_WhenRead_ChecksTheFeatureFlagPathAfterPublishing(string jobName, string publishStep)
    {
        var job = WorkflowFile.Read(RepositoryCheckout.Current, "aot-gate.yml").Job(jobName);
        var steps = job.StepNames().ToList();

        steps.IndexOf(CheckStep).ShouldBeGreaterThan(steps.IndexOf(publishStep));
        steps.IndexOf(publishStep).ShouldBeGreaterThan(-1);
        job.Step(publishStep).ShouldContain("-p:TrimmerSingleWarn=false");
        job.Step(publishStep).ShouldContain("tee aot-publish.log");
        job.Step(CheckStep).ShouldContain($"./{Script} -PublishLog aot-publish.log");
    }
}

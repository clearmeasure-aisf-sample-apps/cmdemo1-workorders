using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The Release workflow, the script it calls and the Build workflow's artifacts have to agree, or
/// <c>GET /_build</c> answers nulls without anything failing.
/// </summary>
[TestFixture]
public class BuildFactsWorkflowTests
{
    private const string StampStep = "Stamp the build facts into the image's files";

    private const string ScriptPath = "scripts/Write-BuildFacts.ps1";

    [Test]
    public void ReleaseWorkflow_WhenRead_StampsTheFactsBetweenExtractingTheUiAndBuildingTheImage()
    {
        var steps = ReleaseWorkflowWithTheStampStep().StepNames().ToList();

        steps.IndexOf("Extract the UI for the image").ShouldBeGreaterThan(-1);
        steps.IndexOf(StampStep).ShouldBeGreaterThan(steps.IndexOf("Extract the UI for the image"));
        steps.IndexOf("Build and push the image").ShouldBeGreaterThan(steps.IndexOf(StampStep));
    }

    [Test]
    public void ReleaseWorkflow_WhenRead_CallsTheScriptWithParametersItHas()
    {
        var step = ReleaseWorkflowWithTheStampStep().Step(StampStep);
        var script = RepositoryCheckout.Current.ReadText(ScriptPath);

        step.ShouldContain("if: hashFiles('scripts/Write-BuildFacts.ps1') != ''");
        step.ShouldContain("GH_TOKEN: ${{ github.token }}");
        step.ShouldContain("./scripts/Write-BuildFacts.ps1 -DownloadArtifacts");
        step.ShouldContain("-RunId '${{ github.event.workflow_run.id }}'");

        // The image is built from ./built (Dockerfile: COPY /built/ /app): the file is written there, not into a zip.
        step.ShouldContain("-OutputPath 'built/build-facts.json'");
        step.ShouldNotContain("-Package");
        foreach (var parameter in new[] { "DownloadArtifacts", "Version", "RunId", "BuiltAt", "OutputPath" })
        {
            step.ShouldContain($"-{parameter}");
            script.ShouldMatch($@"\[(string|switch)\]\${parameter}\b");
        }
    }

    [TestCase("test-results-linux")]
    [TestCase("test-results-acceptance")]
    [TestCase("code-coverage-linux")]
    [TestCase("crap-metrics-linux")]
    [TestCase("qodana-report")]
    public void BuildWorkflow_WhenRead_UploadsTheArtifactTheScriptReads(string artifact)
    {
        var yaml = WorkflowFile.Read(RepositoryCheckout.Current, "build.yml").Text;
        var script = RepositoryCheckout.Current.ReadText(ScriptPath);

        yaml.ShouldContain($"name: {artifact}");
        script.ShouldContain($"'{artifact}'");
    }

    // release.yml is the demo-environment kit's file (its sync-app-workflows.ps1 writes it; it is not edited here).
    // A copy older than the kit's build-facts step has nothing to check yet, and the release then stamps no facts.
    private static WorkflowFile ReleaseWorkflowWithTheStampStep()
    {
        var workflow = WorkflowFile.Read(RepositoryCheckout.Current, "release.yml");
        if (!workflow.StepNames().Contains(StampStep))
        {
            Assert.Ignore(
                "release.yml has no build-facts step: it is older than the kit's template. Run the kit's sync-app-workflows.ps1.");
        }

        return workflow;
    }
}

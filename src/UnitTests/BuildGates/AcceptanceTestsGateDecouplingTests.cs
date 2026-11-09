using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class AcceptanceTestsGateDecouplingTests
{
    [TestCase("acceptance-tests")]
    [TestCase("acceptance-tests-arm")]
    public void BuildWorkflow_AcceptanceJobIfExpression_DoesNotContainSuccess(string jobId)
    {
        var ifLine = IfLineOf(BuildJob(jobId));

        ifLine.ShouldNotContain("success()");
        ifLine.ShouldContain("needs.changes.outputs.code == 'true'");
    }

    [TestCase("build-linux", "LINUX")]
    [TestCase("build-sqlite", "SQLITE")]
    [TestCase("integration-build-arm", "ARM")]
    [TestCase("code-analysis", "ANALYSIS")]
    [TestCase("qodana", "QODANA")]
    [TestCase("build-windows", "WINDOWS")]
    [TestCase("acceptance-tests", "ACCEPTANCE")]
    [TestCase("acceptance-tests-arm", "ACCEPTANCE_ARM")]
    public void BuildWorkflow_BuildResultJob_NeedsAndChecksEveryGatedJob(string gatedJob, string envVar)
    {
        var buildResult = BuildJob("build-result");

        buildResult.ShouldContain($"- {gatedJob}");
        buildResult.ShouldContain($"{envVar}: ${{{{ needs.{gatedJob}.result }}}}");
        buildResult.ShouldContain($"\"${{{envVar}}}\"");
        buildResult.ShouldContain("if: always() && !cancelled()");
    }

    private static string BuildJob(string jobId)
    {
        return WorkflowFile.Read(RepositoryCheckout.Current, "build.yml").Job(jobId).Text;
    }

    private static string IfLineOf(string job)
    {
        return job.Split('\n').First(line => line.StartsWith("    if:", StringComparison.Ordinal));
    }
}

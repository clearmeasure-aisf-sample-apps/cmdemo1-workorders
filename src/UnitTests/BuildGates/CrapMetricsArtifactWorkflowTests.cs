using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapMetricsArtifactWorkflowTests
{
    private const string EnforceStep = "Enforce CRAP (production)";
    private const string SummaryStep = "Publish CRAP summary to job summary";
    private const string UploadStep = "Upload CRAP metrics";

    private static readonly string[] CrapReports =
    [
        "crap-metrics/crap-summary.md",
        "crap-metrics/crap-report.json",
        "crap-metrics/crap-by-file.json",
        "crap-metrics/crap-by-file.csv",
        "crap-metrics/crap-production-violations.json",
    ];

    [Test]
    public void BuildWorkflow_WhenRead_PublishesCrapSummaryAndArtifactAfterEnforceStep()
    {
        var linuxJob = WorkflowFile.Read(RepositoryCheckout.Current, "build.yml").Job("build-linux");

        ShouldRunInOrder(linuxJob, EnforceStep, SummaryStep, UploadStep);
        ShouldWriteTheSummaryToTheJobSummaryEvenOnFailure(linuxJob.Step(SummaryStep));
        ShouldUploadEveryCrapReportForThirtyDaysEvenOnFailure(linuxJob.Step(UploadStep));
    }

    [Test]
    public void CrapAuditDoc_WhenRead_DocumentsJobSummaryAndArtifact()
    {
        var doc = RepositoryCheckout.Current.ReadText("docs/crap-score-audit.md");

        doc.ShouldContain("GITHUB_STEP_SUMMARY");
        doc.ShouldContain("job summary");
        doc.ShouldContain("crap-metrics/crap-summary.md");
        doc.ShouldContain("Integration Build (SQL container)");
        doc.ShouldContain(SummaryStep);
        doc.ShouldContain(UploadStep);
        doc.ShouldContain("crap-metrics-linux");
        doc.ShouldContain("if: always()");
    }

    private static void ShouldRunInOrder(WorkflowFile job, params string[] stepNames)
    {
        var steps = job.StepNames().ToList();
        var positions = stepNames.Select(name => steps.IndexOf(name)).ToArray();

        positions.ShouldAllBe(position => position >= 0, string.Join(", ", steps));
        positions.ShouldBe(positions.Order().ToArray());
    }

    private static void ShouldWriteTheSummaryToTheJobSummaryEvenOnFailure(string summaryStep)
    {
        summaryStep.ShouldContain("if: always()");
        summaryStep.ShouldContain("GITHUB_STEP_SUMMARY");
        summaryStep.ShouldContain("crap-metrics/crap-summary.md");
        summaryStep.ShouldNotContain("uses: actions/upload-artifact");
    }

    private static void ShouldUploadEveryCrapReportForThirtyDaysEvenOnFailure(string uploadStep)
    {
        uploadStep.ShouldContain("uses: actions/upload-artifact");
        uploadStep.ShouldContain("if: always()");
        uploadStep.ShouldContain("name: crap-metrics-linux");
        uploadStep.ShouldContain("retention-days: 30");
        CrapReports.ShouldAllBe(report => uploadStep.Contains(report));
    }
}

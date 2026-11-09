using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class WorkflowFileTests
{
    [Test]
    public void Job_WhenWorkflowHasSeveralJobs_ReturnsOnlyThatJob()
    {
        using var tree = WorkflowFileMother.CheckoutWithSampleWorkflow();

        var job = WorkflowFile.Read(tree.Checkout, WorkflowFileMother.FileName).Job("build");

        job.Text.ShouldStartWith("  build:");
        job.Text.ShouldContain("run: dotnet build");
        job.Text.ShouldNotContain("deploy:");
    }

    [Test]
    public void Job_WhenAKeyOutsideJobsHasTheSameName_IgnoresIt()
    {
        using var tree = WorkflowFileMother.CheckoutWithSampleWorkflow();

        var job = WorkflowFile.Read(tree.Checkout, WorkflowFileMother.FileName).Job("push");

        job.Text.ShouldContain("Push the image");
    }

    [Test]
    public void StepNames_WhenJobMixesNamedAndUnnamedSteps_ListsNamesInRunOrder()
    {
        using var tree = WorkflowFileMother.CheckoutWithSampleWorkflow();

        var steps = WorkflowFile.Read(tree.Checkout, WorkflowFileMother.FileName).Job("build").StepNames();

        steps.ShouldBe(["Restore", "Build", "Upload"]);
    }

    [Test]
    public void Step_WhenNamed_ReturnsItsBlockWithoutTheNextStepOrJob()
    {
        using var tree = WorkflowFileMother.CheckoutWithSampleWorkflow();

        var step = WorkflowFile.Read(tree.Checkout, WorkflowFileMother.FileName).Step("Upload");

        step.ShouldContain("name: build-output");
        step.ShouldContain("retention-days: 7");
        step.ShouldNotContain("push:");
    }

    [Test]
    public void Job_WhenMissing_FailsWithItsName()
    {
        using var tree = WorkflowFileMother.CheckoutWithSampleWorkflow();
        var workflow = WorkflowFile.Read(tree.Checkout, WorkflowFileMother.FileName);

        var error = Should.Throw<InvalidOperationException>(() => workflow.Job("release"));

        error.Message.ShouldContain("release");
    }
}

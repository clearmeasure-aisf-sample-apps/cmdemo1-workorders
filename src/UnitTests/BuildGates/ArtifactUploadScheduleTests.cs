using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class ArtifactUploadScheduleTests
{
    [Test]
    public void BuildWorkflow_UploadSteps_TimeOutInThreeMinutesAndStillRunAfterFailure()
    {
        var workflow = RepositoryCheckout.Current.ReadText(".github/workflows/build.yml");

        ArtifactUploadSchedule.Problems(workflow).ShouldBeEmpty();
    }
}

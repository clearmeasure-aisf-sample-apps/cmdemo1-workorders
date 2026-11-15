using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentWorkflowJobsTests
{
    private const string TwoJobYaml = """
        jobs:
          build:
            runs-on: ubuntu-latest
          upload:
            permissions:
              id-token: write
        """;

    [Test]
    public void All_WhenWorkflowHasTwoJobs_ReturnsBothIds()
    {
        var jobs = PrEnvironmentWorkflowJobs.All(TwoJobYaml);

        jobs.Count.ShouldBe(2);
        jobs[0].Id.ShouldBe("build");
        jobs[1].Id.ShouldBe("upload");
    }

    [Test]
    public void WithIdTokenWrite_WhenOnlyUploadHasOidc_ReturnsUploadAlone()
    {
        var jobs = PrEnvironmentWorkflowJobs.WithIdTokenWrite(TwoJobYaml);

        jobs.Count.ShouldBe(1);
        jobs[0].Id.ShouldBe("upload");
    }
}

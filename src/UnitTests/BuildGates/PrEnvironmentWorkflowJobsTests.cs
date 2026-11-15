using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentWorkflowJobsTests
{
    [Test]
    public void All_WhenWorkflowHasTwoJobs_ReturnsBothIds()
    {
        const string yaml = """
            jobs:
              build:
                runs-on: ubuntu-latest
              upload:
                permissions:
                  id-token: write
            """;

        var jobs = PrEnvironmentWorkflowJobs.All(yaml);

        jobs.Count.ShouldBe(2);
        jobs[0].Id.ShouldBe("build");
        jobs[1].Id.ShouldBe("upload");
    }

    [Test]
    public void WithIdTokenWrite_WhenOnlyUploadHasOidc_ReturnsUploadAlone()
    {
        const string yaml = """
            jobs:
              build:
                runs-on: ubuntu-latest
              upload:
                permissions:
                  id-token: write
            """;

        var jobs = PrEnvironmentWorkflowJobs.WithIdTokenWrite(yaml);

        jobs.Count.ShouldBe(1);
        jobs[0].Id.ShouldBe("upload");
    }
}

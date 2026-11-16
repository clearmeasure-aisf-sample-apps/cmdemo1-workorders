using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ApiProblemsTests
{
    [Test]
    public void BadRequest_Should_ReturnA400ProblemWithTheDetail()
    {
        var problem = ApiProblems.BadRequest("Text is required.").ShouldBeOfType<ProblemHttpResult>();

        problem.StatusCode.ShouldBe(400);
        problem.ProblemDetails.Detail.ShouldBe("Text is required.");
    }
}

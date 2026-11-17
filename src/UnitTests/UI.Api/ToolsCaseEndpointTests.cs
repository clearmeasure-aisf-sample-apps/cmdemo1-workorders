using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsCaseEndpointTests
{
    [Test]
    public void Post_Should_ReturnUpperText_When_ToIsUpper()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("Hello", "upper"))).ShouldBe("HELLO");
    }

    [Test]
    public void Post_Should_ReturnLowerText_When_ToIsLower()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("Hello", "lower"))).ShouldBe("hello");
    }

    [Test]
    public void Post_Should_ReturnTitleText_When_ToIsTitle()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("hello WORLD", "title"))).ShouldBe("Hello World");
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("hELLo-world", "title"))).ShouldBe("Hello-world");
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("   ", "title"))).ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_ReturnKebabText_When_ToIsKebab()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("Hello, World!", "kebab"))).ShouldBe("hello-world");
    }

    [Test]
    public void Post_Should_ReturnSnakeText_When_ToIsSnake()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("Hello, World!", "snake"))).ShouldBe("hello_world");
    }

    [Test]
    public void Post_Should_ReturnUpperText_When_ToIgnoresCase()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest("Hello", " UPPER "))).ShouldBe("HELLO");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_TextIsEmpty()
    {
        ReadText(ToolsCaseEndpoint.Post(new CaseRequest(string.Empty, "upper"))).ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_ToIsUnknown()
    {
        var unknown = ToolsCaseEndpoint.Post(new CaseRequest("Hello", "camel"));
        var missing = ToolsCaseEndpoint.Post(new CaseRequest("Hello", null));
        var blank = ToolsCaseEndpoint.Post(new CaseRequest("Hello", "  "));

        unknown.ShouldBeBadRequestProblem();
        missing.ShouldBeBadRequestProblem();
        blank.ShouldBeBadRequestProblem();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextNull()
    {
        var nullBody = ToolsCaseEndpoint.Post(null);
        var nullText = ToolsCaseEndpoint.Post(new CaseRequest(null, "upper"));

        nullBody.ShouldBeBadRequestProblem();
        nullText.ShouldBeBadRequestProblem();
    }

    [Test]
    public void Endpoint_Should_AllowAnonymousRateLimitedRoutes() =>
        MappedApiEndpoints.ShouldBeAnonymousAndRateLimited("tools/case", HttpMethods.Post);

    private static string ReadText(IResult result)
    {
        var ok = result.ShouldBeOkResult();
        return ok.Value.ShouldBeOfType<CaseResponse>().Text;
    }
}

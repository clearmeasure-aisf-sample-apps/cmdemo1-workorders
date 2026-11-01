using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsCaseControllerTests
{
    [Test]
    public void Post_Should_ReturnUpperText_When_ToIsUpper()
    {
        ReadText(CreateController().Post(new CaseRequest("Hello", "upper"))).ShouldBe("HELLO");
    }

    [Test]
    public void Post_Should_ReturnLowerText_When_ToIsLower()
    {
        ReadText(CreateController().Post(new CaseRequest("Hello", "lower"))).ShouldBe("hello");
    }

    [Test]
    public void Post_Should_ReturnTitleText_When_ToIsTitle()
    {
        ReadText(CreateController().Post(new CaseRequest("hello WORLD", "title"))).ShouldBe("Hello World");
        ReadText(CreateController().Post(new CaseRequest("hELLo-world", "title"))).ShouldBe("Hello-world");
        ReadText(CreateController().Post(new CaseRequest("   ", "title"))).ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_ReturnKebabText_When_ToIsKebab()
    {
        ReadText(CreateController().Post(new CaseRequest("Hello, World!", "kebab"))).ShouldBe("hello-world");
    }

    [Test]
    public void Post_Should_ReturnSnakeText_When_ToIsSnake()
    {
        ReadText(CreateController().Post(new CaseRequest("Hello, World!", "snake"))).ShouldBe("hello_world");
    }

    [Test]
    public void Post_Should_ReturnUpperText_When_ToIgnoresCase()
    {
        ReadText(CreateController().Post(new CaseRequest("Hello", " UPPER "))).ShouldBe("HELLO");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_TextIsEmpty()
    {
        ReadText(CreateController().Post(new CaseRequest(string.Empty, "upper"))).ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_ToIsUnknown()
    {
        var unknown = CreateController().Post(new CaseRequest("Hello", "camel"));
        var missing = CreateController().Post(new CaseRequest("Hello", null));
        var blank = CreateController().Post(new CaseRequest("Hello", "  "));

        foreach (var result in new[] { unknown, missing, blank })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextNull()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new CaseRequest(null, "upper"));

        foreach (var result in new[] { nullBody, nullText })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Controller_Should_AllowAnonymousRateLimitedRoutes()
    {
        var controllerType = typeof(ToolsCaseController);
        var routes = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        routes.ShouldContain("api/tools/case");
        routes.ShouldContain($"{ApiRoutes.VersionedApiPrefix}/tools/case");
        routes.Length.ShouldBe(2);

        var rateLimit = controllerType.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .Single();
        rateLimit.PolicyName.ShouldBe(ApiRateLimiting.PolicyName);

        var post = controllerType.GetMethod(nameof(ToolsCaseController.Post));
        post.ShouldNotBeNull();
        post.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).ShouldNotBeEmpty();
    }

    private static string ReadText(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        return ok.Value.ShouldBeOfType<CaseResponse>().Text;
    }

    private static ToolsCaseController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

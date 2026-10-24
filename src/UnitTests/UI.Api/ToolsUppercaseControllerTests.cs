using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsUppercaseControllerTests
{
    [Test]
    public void Post_Should_ReturnUpperCase_When_TextProvided()
    {
        var payload = ReadPayload(CreateController().Post(new UppercaseRequest("Hello, World!")));

        payload.Text.ShouldBe("HELLO, WORLD!");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_EmptyString()
    {
        var payload = ReadPayload(CreateController().Post(new UppercaseRequest("")));

        payload.Text.ShouldBe("");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_TextMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new UppercaseRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            ReadPayload(result).Text.ShouldBe("");
        }
    }

    [Test]
    public void Post_Should_KeepWhitespace_When_TextIsNotTrimmed()
    {
        var payload = ReadPayload(CreateController().Post(new UppercaseRequest("  a  ")));

        payload.Text.ShouldBe("  A  ");
    }

    [Test]
    public void Post_Should_UseInvariantCulture_When_TurkishI()
    {
        var payload = ReadPayload(CreateController().Post(new UppercaseRequest("i")));

        payload.Text.ShouldBe("I");
    }

    [Test]
    public void Controller_Should_AllowAnonymousRateLimitedRoutes()
    {
        var controllerType = typeof(ToolsUppercaseController);
        var routes = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        routes.ShouldContain("api/tools/uppercase");
        routes.ShouldContain($"{ApiRoutes.VersionedApiPrefix}/tools/uppercase");
        routes.Length.ShouldBe(2);

        var rateLimit = controllerType.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .Single();
        rateLimit.PolicyName.ShouldBe(ApiRateLimiting.PolicyName);

        var post = controllerType.GetMethod(nameof(ToolsUppercaseController.Post));
        post.ShouldNotBeNull();
        post.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).ShouldNotBeEmpty();
    }

    private static UppercaseResponse ReadPayload(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        return ok.Value.ShouldBeOfType<UppercaseResponse>();
    }

    private static ToolsUppercaseController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

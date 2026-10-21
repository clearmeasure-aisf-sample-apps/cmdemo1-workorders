using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsReverseControllerTests
{
    [Test]
    public void Post_Should_ReturnEmptyString_When_TextIsEmpty()
    {
        var result = CreateController().Post(new ReverseRequest(string.Empty));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_ReturnEmptyString_When_TextIsMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new ReverseRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
            payload.Reversed.ShouldBe(string.Empty);
        }
    }

    [Test]
    public void Post_Should_ReverseText_When_TextIsAscii()
    {
        var result = CreateController().Post(new ReverseRequest("work orders"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe("sredro krow");
    }

    [Test]
    public void Post_Should_KeepAccentedTextElementsIntact_When_TextIsAccented()
    {
        var result = CreateController().Post(new ReverseRequest("cafe\u0301"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe("e\u0301fac");
    }

    private static ToolsReverseController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

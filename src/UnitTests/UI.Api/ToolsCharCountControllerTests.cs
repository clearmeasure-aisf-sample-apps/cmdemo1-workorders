using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsCharCountControllerTests
{
    [Test]
    public void Post_ShouldReturnZeroCounts_WhenTextIsEmpty()
    {
        var result = CreateController().Post(new CharCountRequest(string.Empty));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(0);
        payload.NonWhitespace.ShouldBe(0);
    }

    [Test]
    public void Post_ShouldReturnZeroCounts_WhenTextIsMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new CharCountRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
            payload.Characters.ShouldBe(0);
            payload.NonWhitespace.ShouldBe(0);
        }
    }

    [Test]
    public void Post_ShouldCountTextElements_WhenAsciiTextProvided()
    {
        var result = CreateController().Post(new CharCountRequest("hello world"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(11);
        payload.NonWhitespace.ShouldBe(10);
    }

    [Test]
    public void Post_ShouldCountNoNonWhitespaceTextElements_WhenTextContainsOnlySpaces()
    {
        var result = CreateController().Post(new CharCountRequest(" \t\n"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(3);
        payload.NonWhitespace.ShouldBe(0);
    }

    [Test]
    public void Post_ShouldCountEmojiAsOneTextElement_WhenEmojiProvided()
    {
        var result = CreateController().Post(new CharCountRequest("😀"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(1);
        payload.NonWhitespace.ShouldBe(1);
    }

    private static ToolsCharCountController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

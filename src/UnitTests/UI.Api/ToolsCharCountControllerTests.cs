using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsCharCountControllerTests
{
    [Test]
    public void Post_Should_ReturnZeroCounts_When_TextIsMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new CharCountRequest(null));
        var emptyText = CreateController().Post(new CharCountRequest(string.Empty));

        foreach (var result in new[] { nullBody, nullText, emptyText })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
            payload.Characters.ShouldBe(0);
            payload.NonWhitespace.ShouldBe(0);
        }
    }

    [Test]
    public void Post_Should_CountTextElements_When_AsciiText()
    {
        var result = CreateController().Post(new CharCountRequest("Hello"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(5);
        payload.NonWhitespace.ShouldBe(5);
    }

    [Test]
    public void Post_Should_ExcludeWhitespace_When_TextContainsSpaces()
    {
        var result = CreateController().Post(new CharCountRequest("a b "));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<CharCountResponse>();
        payload.Characters.ShouldBe(4);
        payload.NonWhitespace.ShouldBe(2);
    }

    [Test]
    public void Post_Should_CountEmojiAsOneTextElement_When_EmojiText()
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

using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsWordCountControllerTests
{
    [Test]
    public void Post_Should_ReturnWordCount_When_TextProvided()
    {
        var result = CreateController().Post(new WordCountRequest("hello world\nfoo"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.Words.ShouldBe(3);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_EmptyString()
    {
        var result = CreateController().Post(new WordCountRequest(""));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.Words.ShouldBe(0);
    }

    [Test]
    public void Post_Should_ReturnOne_When_SingleWord()
    {
        var result = CreateController().Post(new WordCountRequest("word"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.Words.ShouldBe(1);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_TextMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new WordCountRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
            payload.Words.ShouldBe(0);
        }
    }

    [Test]
    public void Post_Should_CountWhitespaceDelimitedWords_When_MultipleSpacesAndNewlines()
    {
        var result = CreateController().Post(new WordCountRequest("  one   two\nthree\tfour  "));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.Words.ShouldBe(4);
    }

    private static ToolsWordCountController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

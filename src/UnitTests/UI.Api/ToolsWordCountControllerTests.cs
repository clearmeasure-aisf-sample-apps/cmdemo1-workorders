using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsWordCountControllerTests
{
    [Test]
    public void Post_Should_ReturnWordCount_When_TextProvided()
    {
        var result = CreateController().Post(new WordCountRequest("hello world\nfoo"));

        var payload = ReadPayload(result);
        payload.Words.ShouldBe(3);
        payload.Characters.ShouldBe(15);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_EmptyString()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("")));

        payload.Words.ShouldBe(0);
        payload.Characters.ShouldBe(0);
        payload.Lines.ShouldBe(0);
    }

    [Test]
    public void Post_Should_ReturnOne_When_SingleWord()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("word")));

        payload.Words.ShouldBe(1);
        payload.Characters.ShouldBe(4);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_TextMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new WordCountRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var payload = ReadPayload(result);
            payload.Words.ShouldBe(0);
            payload.Characters.ShouldBe(0);
            payload.Lines.ShouldBe(0);
        }
    }

    [Test]
    public void Post_Should_CountWhitespaceDelimitedWords_When_MultipleSpacesAndNewlines()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("  one   two\nthree\tfour  ")));

        payload.Words.ShouldBe(4);
        payload.Characters.ShouldBe(24);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_ReturnZeroWords_When_SpacesOnly()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("   ")));

        payload.Words.ShouldBe(0);
        payload.Characters.ShouldBe(3);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_CountEachLine_When_SeveralLines()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("one\ntwo\nthree")));

        payload.Words.ShouldBe(3);
        payload.Characters.ShouldBe(13);
        payload.Lines.ShouldBe(3);
    }

    [Test]
    public void Post_Should_CountUnicodeText_When_NonAsciiCharacters()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("café 東京")));

        payload.Words.ShouldBe(2);
        payload.Characters.ShouldBe(7);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_CountTrailingBreak_When_TextEndsWithNewline()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("a\n")));

        payload.Words.ShouldBe(1);
        payload.Characters.ShouldBe(2);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_CountOneLineBreak_When_CrLf()
    {
        var payload = ReadPayload(CreateController().Post(new WordCountRequest("a\r\nb")));

        payload.Words.ShouldBe(2);
        payload.Characters.ShouldBe(4);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Controller_Should_AllowAnonymousRateLimitedRoutes()
    {
        var controllerType = typeof(ToolsWordCountController);
        var routes = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        routes.ShouldContain("api/tools/word-count");
        routes.ShouldContain($"{ApiRoutes.VersionedApiPrefix}/tools/word-count");
        routes.Length.ShouldBe(2);

        var rateLimit = controllerType.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .Single();
        rateLimit.PolicyName.ShouldBe(ApiRateLimiting.PolicyName);

        var post = controllerType.GetMethod(nameof(ToolsWordCountController.Post));
        post.ShouldNotBeNull();
        post.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).ShouldNotBeEmpty();
    }

    private static WordCountResponse ReadPayload(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        return ok.Value.ShouldBeOfType<WordCountResponse>();
    }

    private static ToolsWordCountController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

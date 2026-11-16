using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
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
        var result = ToolsWordCountEndpoint.Post(new WordCountRequest("hello world\nfoo"));

        var payload = ReadPayload(result);
        payload.Words.ShouldBe(3);
        payload.Characters.ShouldBe(15);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_EmptyString()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("")));

        payload.Words.ShouldBe(0);
        payload.Characters.ShouldBe(0);
        payload.Lines.ShouldBe(0);
    }

    [Test]
    public void Post_Should_ReturnOne_When_SingleWord()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("word")));

        payload.Words.ShouldBe(1);
        payload.Characters.ShouldBe(4);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_TextMissing()
    {
        var nullBody = ToolsWordCountEndpoint.Post(null);
        var nullText = ToolsWordCountEndpoint.Post(new WordCountRequest(null));

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
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("  one   two\nthree\tfour  ")));

        payload.Words.ShouldBe(4);
        payload.Characters.ShouldBe(24);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_ReturnZeroWords_When_SpacesOnly()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("   ")));

        payload.Words.ShouldBe(0);
        payload.Characters.ShouldBe(3);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_CountEachLine_When_SeveralLines()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("one\ntwo\nthree")));

        payload.Words.ShouldBe(3);
        payload.Characters.ShouldBe(13);
        payload.Lines.ShouldBe(3);
    }

    [Test]
    public void Post_Should_CountUnicodeText_When_NonAsciiCharacters()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("café 東京")));

        payload.Words.ShouldBe(2);
        payload.Characters.ShouldBe(7);
        payload.Lines.ShouldBe(1);
    }

    [Test]
    public void Post_Should_CountTrailingBreak_When_TextEndsWithNewline()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("a\n")));

        payload.Words.ShouldBe(1);
        payload.Characters.ShouldBe(2);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Post_Should_CountOneLineBreak_When_CrLf()
    {
        var payload = ReadPayload(ToolsWordCountEndpoint.Post(new WordCountRequest("a\r\nb")));

        payload.Words.ShouldBe(2);
        payload.Characters.ShouldBe(4);
        payload.Lines.ShouldBe(2);
    }

    [Test]
    public void Endpoint_Should_AllowAnonymousRateLimitedRoutes() =>
        MappedApiEndpoints.ShouldBeAnonymousAndRateLimited("tools/word-count", HttpMethods.Post);

    private static WordCountResponse ReadPayload(IResult result)
    {
        var ok = result.ShouldBeOkResult();
        return ok.Value.ShouldBeOfType<WordCountResponse>();
    }
}

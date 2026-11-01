using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsSlugifyControllerTests
{
    [Test]
    public void Post_Should_ReturnSlug_When_NormalText()
    {
        var result = CreateController().Post(new SlugifyRequest("Hello, World! 2026"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("hello-world-2026");
    }

    [Test]
    public void Post_Should_HandleMultiplePunctuation_When_PunctuationRuns()
    {
        var result = CreateController().Post(new SlugifyRequest("foo---bar!!!baz"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("foo-bar-baz");
    }

    [Test]
    public void Post_Should_TrimLeadingAndTrailing_When_SeparatorsOnEdges()
    {
        var result = CreateController().Post(new SlugifyRequest("---hello-world---"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("hello-world");
    }

    [Test]
    public void Post_Should_ReturnEmptySlug_When_EmptyString()
    {
        var result = CreateController().Post(new SlugifyRequest(""));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_ReturnEmptySlug_When_WhitespaceOnly()
    {
        var result = CreateController().Post(new SlugifyRequest("   \t\n  "));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_CutSlugAt80Characters_When_AlphanumericTextIsLong()
    {
        var result = CreateController().Post(new SlugifyRequest(new string('A', 100)));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe(new string('a', 80));
        payload.Slug.ShouldNotEndWith("-");
    }

    [Test]
    public void Post_Should_DropTrailingHyphen_When_CutLandsOnSeparator()
    {
        var result = CreateController().Post(new SlugifyRequest(string.Join(' ', Enumerable.Repeat("word", 20))));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe(string.Join('-', Enumerable.Repeat("word", 16)));
        payload.Slug.Length.ShouldBeLessThanOrEqualTo(80);
        payload.Slug.ShouldNotEndWith("-");
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextNull()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new SlugifyRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    private static ToolsSlugifyController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

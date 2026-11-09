using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant);

    [Test]
    public void Get_Should_ReturnPlainTextNumber_When_TypeNumber()
    {
        var result = CreateController().Get("number");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        int.TryParse(content.Content, out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnPlainTextAlphanumeric_When_TypeString()
    {
        var result = CreateController().Get("string");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        content.Content.Length.ShouldBe(12);
        content.Content.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    [Test]
    public void Get_Should_ReturnGuidDFormat_When_TypeUuid()
    {
        var result = CreateController().Get("uuid");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        Guid.TryParseExact(content.Content, "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnHexColor_When_TypeColor()
    {
        var result = CreateController().Get("color");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        HexColorRegex.IsMatch(content.Content).ShouldBeTrue();
    }

    [TestCase("UUID")]
    [TestCase("Number")]
    [TestCase("CoLoR")]
    [TestCase("STRING")]
    public void Get_Should_BeCaseInsensitive_When_TypeMixedCase(string type)
    {
        var result = CreateController().Get(type);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_TypeMissing()
    {
        var result = CreateController().Get(null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_TypeUnknown()
    {
        var result = CreateController().Get("banana");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_MinGreaterThanMax()
    {
        var result = CreateController().Post(5, 1);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe("Query parameter 'min' must be less than or equal to 'max'.");
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_MinOrMaxMissing()
    {
        var result = CreateController().Post(null, 4);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe("Query parameters 'min' and 'max' are required.");
    }

    [Test]
    public void Post_Should_ReturnThatValue_When_MinEqualsMax()
    {
        var result = CreateController().Post(7, 7);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBe("7");
    }

    [Test]
    public void Post_Should_ReturnValueInsideInclusiveRange_When_NormalRange()
    {
        var result = CreateController().Post(10, 12);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldNotBeNull();
        int.TryParse(content.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value).ShouldBeTrue();
        value.ShouldBeGreaterThanOrEqualTo(10);
        value.ShouldBeLessThanOrEqualTo(12);
    }

    [Test]
    public void Post_Should_ReturnValueInsideInclusiveRange_When_MaxIsIntMaxValue()
    {
        var result = CreateController().Post(int.MaxValue - 1, int.MaxValue);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.Content.ShouldNotBeNull();
        int.TryParse(content.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value).ShouldBeTrue();
        value.ShouldBeGreaterThanOrEqualTo(int.MaxValue - 1);
        value.ShouldBeLessThanOrEqualTo(int.MaxValue);
    }

    private static ToolsRandomController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

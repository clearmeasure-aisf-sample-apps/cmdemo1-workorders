using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsBase64ControllerTests
{
    [Test]
    public void Post_Should_ReturnEncodedBase64_When_ModeEncodeOrDefault()
    {
        var defaultMode = CreateController().Post(new Base64Request("hello"));
        var explicitEncode = CreateController().Post(new Base64Request("hello", "encode"));

        foreach (var result in new[] { defaultMode, explicitEncode })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<Base64Response>();
            payload.Result.ShouldBe("aGVsbG8=");
        }
    }

    [Test]
    public void Post_Should_ReturnDecodedText_When_ModeDecode()
    {
        var result = CreateController().Post(new Base64Request("aGVsbG8=", "decode"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<Base64Response>();
        payload.Result.ShouldBe("hello");
    }

    [Test]
    public void Post_Should_AcceptModeCaseInsensitive_When_EncodeOrDecode()
    {
        var encode = CreateController().Post(new Base64Request("hello", "ENCODE"));
        var decode = CreateController().Post(new Base64Request("aGVsbG8=", "Decode"));

        var encodeOk = encode.ShouldBeOfType<OkObjectResult>();
        encodeOk.Value.ShouldBeOfType<Base64Response>().Result.ShouldBe("aGVsbG8=");

        var decodeOk = decode.ShouldBeOfType<OkObjectResult>();
        decodeOk.Value.ShouldBeOfType<Base64Response>().Result.ShouldBe("hello");
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_DecodeInvalidBase64()
    {
        var result = CreateController().Post(new Base64Request("not!valid!base64", "decode"));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissingOrEmpty()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new Base64Request(null));
        var emptyText = CreateController().Post(new Base64Request(""));

        foreach (var result in new[] { nullBody, nullText, emptyText })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_ModeUnknown()
    {
        var result = CreateController().Post(new Base64Request("hello", "gzip"));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsBase64Controller CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

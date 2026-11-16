using ClearMeasure.Bootcamp.UI.Api.Endpoints;
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
        var defaultMode = ToolsBase64Endpoint.Post(new Base64Request("hello"));
        var explicitEncode = ToolsBase64Endpoint.Post(new Base64Request("hello", "encode"));

        foreach (var result in new[] { defaultMode, explicitEncode })
        {
            var ok = result.ShouldBeOkResult();
            var payload = ok.Value.ShouldBeOfType<Base64Response>();
            payload.Result.ShouldBe("aGVsbG8=");
        }
    }

    [Test]
    public void Post_Should_ReturnDecodedText_When_ModeDecode()
    {
        var result = ToolsBase64Endpoint.Post(new Base64Request("aGVsbG8=", "decode"));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<Base64Response>();
        payload.Result.ShouldBe("hello");
    }

    [Test]
    public void Post_Should_AcceptModeCaseInsensitive_When_EncodeOrDecode()
    {
        var encode = ToolsBase64Endpoint.Post(new Base64Request("hello", "ENCODE"));
        var decode = ToolsBase64Endpoint.Post(new Base64Request("aGVsbG8=", "Decode"));

        var encodeOk = encode.ShouldBeOkResult();
        encodeOk.Value.ShouldBeOfType<Base64Response>().Result.ShouldBe("aGVsbG8=");

        var decodeOk = decode.ShouldBeOkResult();
        decodeOk.Value.ShouldBeOfType<Base64Response>().Result.ShouldBe("hello");
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_DecodeInvalidBase64()
    {
        var result = ToolsBase64Endpoint.Post(new Base64Request("not!valid!base64", "decode"));

        var objectResult = result.ShouldBeValueResult();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissingOrEmpty()
    {
        var nullBody = ToolsBase64Endpoint.Post(null);
        var nullText = ToolsBase64Endpoint.Post(new Base64Request(null));
        var emptyText = ToolsBase64Endpoint.Post(new Base64Request(""));

        foreach (var result in new[] { nullBody, nullText, emptyText })
        {
            var objectResult = result.ShouldBeValueResult();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_ModeUnknown()
    {
        var result = ToolsBase64Endpoint.Post(new Base64Request("hello", "gzip"));

        var objectResult = result.ShouldBeValueResult();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }
}

using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsHashEndpointTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string AbcMd5 = "900150983cd24fb0d6963f7d28e17f72";
    private const string AbcSha1 = "a9993e364706816aba3e25717850c26c9cd0d89d";
    private const string EmptySha256 =
        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const string CafeSha256 =
        "850f7dc43910ff890f8879c0ed26fe697c93a067ad93a7d50f466a7028a9bf4e";

    [Test]
    public void Post_Should_ReturnSha256LowerHex_When_TextProvided()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest("abc"));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBeNull();
        payload.Sha1.ShouldBeNull();
    }

    [Test]
    public void Post_Should_ReturnSha256_When_EmptyString()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest(""));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(EmptySha256);
    }

    [Test]
    public void Post_Should_IncludeMd5_When_IncludeMd5True()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest("abc", IncludeMd5: true));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBe(AbcMd5);
        payload.Sha1.ShouldBeNull();
    }

    [Test]
    public void Post_Should_IncludeSha1_When_IncludeSha1True()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest("abc", IncludeSha1: true));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBeNull();
        payload.Sha1.ShouldBe(AbcSha1);
    }

    [Test]
    public void Post_Should_IncludeBothOptionalHashes_When_BothFlagsTrue()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest("abc", IncludeMd5: true, IncludeSha1: true));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(AbcSha256);
        payload.Md5.ShouldBe(AbcMd5);
        payload.Sha1.ShouldBe(AbcSha1);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissing()
    {
        var nullBody = ToolsHashEndpoint.Post(null);
        var nullText = ToolsHashEndpoint.Post(new HashRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var objectResult = result.ShouldBeValueResult();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Post_Should_HashUtf8_When_NonAsciiText()
    {
        var result = ToolsHashEndpoint.Post(new HashRequest("café"));

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        payload.Sha256.ShouldBe(CafeSha256);
    }
}

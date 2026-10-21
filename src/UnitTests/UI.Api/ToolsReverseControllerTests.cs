using System.Net;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsReverseControllerTests
{
    [TestCase("/api/tools/reverse")]
    [TestCase("/api/v1.0/tools/reverse")]
    public async Task Should_ReverseTextElements_When_PostedAnonymously(string route)
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();
        var cases = new (string Body, string Expected)[]
        {
            ("{}", string.Empty),
            ("{\"text\":null}", string.Empty),
            ("{\"text\":\"\"}", string.Empty),
            ("{\"text\":\"work orders\"}", "sredro krow"),
            ("{\"text\":\"cafe\\u0301\"}", "e\u0301fac"),
            ("{\"text\":\"a👩‍🔧b\"}", "b👩‍🔧a")
        };

        foreach (var (body, expected) in cases)
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(route, content);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            document.RootElement.GetProperty("reversed").GetString().ShouldBe(expected);
        }
    }

    [Test]
    public void Post_Should_ReturnEmptyString_When_TextIsEmpty()
    {
        var result = CreateController().Post(new ReverseRequest(string.Empty));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe(string.Empty);
    }

    [Test]
    public void Post_Should_ReturnEmptyString_When_TextIsMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new ReverseRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var ok = result.ShouldBeOfType<OkObjectResult>();
            var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
            payload.Reversed.ShouldBe(string.Empty);
        }
    }

    [Test]
    public void Post_Should_ReverseText_When_TextIsAscii()
    {
        var result = CreateController().Post(new ReverseRequest("work orders"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe("sredro krow");
    }

    [Test]
    public void Post_Should_KeepAccentedTextElementsIntact_When_TextIsAccented()
    {
        var result = CreateController().Post(new ReverseRequest("cafe\u0301"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ReverseResponse>();
        payload.Reversed.ShouldBe("e\u0301fac");
    }

    private static ToolsReverseController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}

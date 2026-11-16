using System.Globalization;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsUppercaseEndpointTests
{
    [Test]
    public void Post_Should_ReturnUpperCase_When_TextProvided()
    {
        var payload = ReadPayload(ToolsUppercaseEndpoint.Post(new UppercaseRequest("Hello, World!")));

        payload.Text.ShouldBe("HELLO, WORLD!");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_EmptyString()
    {
        var payload = ReadPayload(ToolsUppercaseEndpoint.Post(new UppercaseRequest("")));

        payload.Text.ShouldBe("");
    }

    [Test]
    public void Post_Should_ReturnEmptyText_When_TextMissing()
    {
        var nullBody = ToolsUppercaseEndpoint.Post(null);
        var nullText = ToolsUppercaseEndpoint.Post(new UppercaseRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            ReadPayload(result).Text.ShouldBe("");
        }
    }

    [Test]
    public void Post_Should_KeepWhitespace_When_TextIsNotTrimmed()
    {
        var payload = ReadPayload(ToolsUppercaseEndpoint.Post(new UppercaseRequest("  a  ")));

        payload.Text.ShouldBe("  A  ");
    }

    [Test]
    public void Post_Should_UseInvariantCulture_When_CultureIsTurkish()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        try
        {
            "i".ToUpper(CultureInfo.CurrentCulture).ShouldBe("İ");
            var payload = ReadPayload(ToolsUppercaseEndpoint.Post(new UppercaseRequest("i")));
            payload.Text.ShouldBe("I");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Endpoint_Should_AllowAnonymousRateLimitedRoutes() =>
        MappedApiEndpoints.ShouldBeAnonymousAndRateLimited("tools/uppercase", HttpMethods.Post);

    private static UppercaseResponse ReadPayload(IResult result)
    {
        var ok = result.ShouldBeOkResult();
        return ok.Value.ShouldBeOfType<UppercaseResponse>();
    }
}

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Isolated-factory API-key tests for the tools/random endpoint. Extracted from
/// <see cref="ToolsRandomEndpointIntegrationTests"/> to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from the original fixture, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsRandomEndpointIsolatedFactoryIntegrationTests
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant);
    private static readonly Regex AlphanumericRegex = new("^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant);

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/random?type=uuid");
        await AssertValidPayloadAsync(unversioned, "uuid");

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=color");
        await AssertValidPayloadAsync(versioned, "color");
    }

    private static async Task AssertValidPayloadAsync(HttpResponseMessage response, string type)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotBeNullOrWhiteSpace();

        switch (type.ToLowerInvariant())
        {
            case "number":
                int.TryParse(body, out _).ShouldBeTrue();
                break;
            case "string":
                body.Length.ShouldBe(12);
                AlphanumericRegex.IsMatch(body).ShouldBeTrue();
                break;
            case "uuid":
                Guid.TryParseExact(body, "D", out _).ShouldBeTrue();
                break;
            case "color":
                HexColorRegex.IsMatch(body).ShouldBeTrue();
                break;
            default:
                Assert.Fail($"Unexpected type under test: {type}");
                break;
        }
    }
}

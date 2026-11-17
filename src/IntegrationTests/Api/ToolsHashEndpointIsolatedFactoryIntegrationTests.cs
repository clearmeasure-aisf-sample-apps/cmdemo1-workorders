using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Isolated-factory API-key tests for the tools/hash endpoint. Extracted from
/// <see cref="ToolsHashEndpointIntegrationTests"/> to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from the original fixture, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsHashEndpointIsolatedFactoryIntegrationTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/hash", new { text = "abc" });
        await AssertSha256OnlyAsync(unversioned, AbcSha256);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "abc" });
        await AssertSha256OnlyAsync(versioned, AbcSha256);
    }

    private static async Task AssertSha256OnlyAsync(HttpResponseMessage response, string expectedSha256)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("sha256").GetString().ShouldBe(expectedSha256);
        root.TryGetProperty("md5", out _).ShouldBeFalse();
        root.TryGetProperty("sha1", out _).ShouldBeFalse();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Isolated-factory API-key tests for the tools/word-count endpoint. Extracted from
/// <see cref="ToolsWordCountEndpointIntegrationTests"/> to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from the original fixture, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsWordCountEndpointIsolatedFactoryIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/word-count", new { text = "test" });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/word-count", new { text = "test" });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

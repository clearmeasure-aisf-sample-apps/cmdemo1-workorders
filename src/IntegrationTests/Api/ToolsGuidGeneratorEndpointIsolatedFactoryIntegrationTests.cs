using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Isolated-factory API-key tests for the tools/guid-generator endpoint. Extracted from
/// <see cref="ToolsGuidGeneratorEndpointIntegrationTests"/> to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from the original fixture, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsGuidGeneratorEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsync("/api/tools/guid-generator", null);
        await AssertGuidArrayAsync(unversioned, expectedCount: 1);

        var versioned = await client.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);
        await AssertGuidArrayAsync(versioned, expectedCount: 2);
    }

    private static async Task AssertGuidArrayAsync(HttpResponseMessage response, int expectedCount)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await response.Content.ReadFromJsonAsync<string[]>();
        guids.ShouldNotBeNull();
        guids.Length.ShouldBe(expectedCount);
        foreach (var guid in guids)
        {
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
        }
    }
}

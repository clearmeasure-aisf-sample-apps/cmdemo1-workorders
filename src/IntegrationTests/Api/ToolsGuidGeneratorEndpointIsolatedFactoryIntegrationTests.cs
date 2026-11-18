using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/guid-generator endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="ToolsGuidGeneratorEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsGuidGeneratorEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostUnversioned_DefaultCount()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/tools/guid-generator", null);
        await ToolsGuidGeneratorEndpointIntegrationTests.AssertGuidArrayAsync(response, 1);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostVersioned_WithCount()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);
        await ToolsGuidGeneratorEndpointIntegrationTests.AssertGuidArrayAsync(response, 2);
    }
}

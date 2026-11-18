using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/hash endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="ToolsHashEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsHashEndpointIsolatedFactoryIntegrationTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostUnversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tools/hash", new { text = "abc" });
        await ToolsHashEndpointIntegrationTests.AssertSha256OnlyAsync(response, AbcSha256);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostVersioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1.0/tools/hash", new { text = "abc" });
        await ToolsHashEndpointIntegrationTests.AssertSha256OnlyAsync(response, AbcSha256);
    }
}

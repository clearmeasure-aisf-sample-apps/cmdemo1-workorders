using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the timestamp-converter endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="TimestampConverterEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class TimestampConverterEndpointIsolatedFactoryIntegrationTests
{
    private const long KnownUnixSeconds = 1_700_000_000L;
    private const string KnownIso = "2023-11-14T22:13:20Z";

    [Test]
    public async Task Should_Return200WithoutApiKey_When_GetUnversioned_WithUnix()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/tools/timestamp-converter?unix={KnownUnixSeconds}");
        await TimestampConverterEndpointIntegrationTests.AssertGoldenAsync(response);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_GetVersioned_WithIso()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1.0/tools/timestamp-converter?iso={Uri.EscapeDataString(KnownIso)}");
        await TimestampConverterEndpointIntegrationTests.AssertGoldenAsync(response);
    }
}

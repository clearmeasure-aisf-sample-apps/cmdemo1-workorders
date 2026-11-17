using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the ping endpoint. Extracted into their own fixture
/// to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/> being active
/// simultaneously with the shared factory from <see cref="PingEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class PingEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_Return200WithoutApiKey_When_PingAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/ping");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadAsStringAsync()).ShouldBe("pong");

        var versioned = await client.GetAsync("/api/v1.0/ping");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadAsStringAsync()).ShouldBe("pong");
    }
}

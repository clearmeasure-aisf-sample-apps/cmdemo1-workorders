using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/work-order-statuses endpoint. Extracted into
/// their own fixture to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/>
/// being active simultaneously with the shared factory from
/// <see cref="ToolsWorkOrderStatusesEndpointIntegrationTests"/>, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsWorkOrderStatusesEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/work-order-statuses");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/work-order-statuses");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

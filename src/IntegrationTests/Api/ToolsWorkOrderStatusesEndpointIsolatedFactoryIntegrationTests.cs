using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/work-order-statuses endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="ToolsWorkOrderStatusesEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsWorkOrderStatusesEndpointIsolatedFactoryIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200WithoutApiKey_When_GetUnversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/work-order-statuses");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        statuses.ShouldNotBeNull();
        statuses.Length.ShouldBe(5);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_GetVersioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/tools/work-order-statuses");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        statuses.ShouldNotBeNull();
        statuses.Length.ShouldBe(5);
    }
}

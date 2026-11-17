using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/case endpoint. Extracted into its own fixture
/// to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/> being active
/// simultaneously with the shared factory from <see cref="ToolsCaseEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsCaseEndpointApiKeyIntegrationTests
{
    [Test]
    public async Task Should_Return200WithUpper_When_PostUnversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tools/case", new { text = "Hello", to = "upper" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        json.GetProperty("text").GetString().ShouldBe("HELLO");
    }

    [Test]
    public async Task Should_Return200WithLower_When_PostVersioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1.0/tools/case", new { text = "Hello", to = "lower" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        json.GetProperty("text").GetString().ShouldBe("hello");
    }
}

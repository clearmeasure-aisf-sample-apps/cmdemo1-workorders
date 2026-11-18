using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/slugify endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="ToolsSlugifyEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsSlugifyEndpointIsolatedFactoryIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostUnversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tools/slugify", new { text = "Hello, World!" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("slug").GetString().ShouldBe("hello-world");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostVersioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1.0/tools/slugify", new { text = "foo---bar" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("slug").GetString().ShouldBe("foo-bar");
    }
}

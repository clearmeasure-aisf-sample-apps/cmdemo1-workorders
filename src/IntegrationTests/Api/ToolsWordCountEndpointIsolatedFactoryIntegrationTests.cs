using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the tools/word-count endpoint.
/// Extracted into its own fixture to avoid a second
/// <see cref="WebApplicationFactory{TEntryMarker}"/> being active simultaneously
/// with the shared factory from <see cref="ToolsWordCountEndpointIntegrationTests"/>,
/// which races on the global <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class ToolsWordCountEndpointIsolatedFactoryIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostUnversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tools/word-count", new { text = "hello world" });
        await AssertWordCountAsync(response, 2, 11, 1);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_PostVersioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1.0/tools/word-count", new { text = "one two" });
        await AssertWordCountAsync(response, 2, 7, 1);
    }

    private static async Task AssertWordCountAsync(
        HttpResponseMessage response, int expectedWords, int expectedCharacters, int expectedLines)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.EnumerateObject().Select(property => property.Name).ShouldBe(["words", "characters", "lines"]);
        json.GetProperty("words").GetInt32().ShouldBe(expectedWords);
        json.GetProperty("characters").GetInt32().ShouldBe(expectedCharacters);
        json.GetProperty("lines").GetInt32().ShouldBe(expectedLines);
    }
}

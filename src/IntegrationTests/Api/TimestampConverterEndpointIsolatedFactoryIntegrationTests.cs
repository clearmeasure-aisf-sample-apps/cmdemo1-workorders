using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for the timestamp-converter endpoint. Extracted into their own
/// fixture to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/> being active
/// simultaneously with the shared factory from
/// <see cref="TimestampConverterEndpointIntegrationTests"/>, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class TimestampConverterEndpointIsolatedFactoryIntegrationTests
{
    private const long KnownUnixSeconds = 1_700_000_000L;
    private const string KnownIso = "2023-11-14T22:13:20Z";
    private const string KnownHuman = "Tuesday, 14 November 2023 22:13:20 UTC";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync($"/api/tools/timestamp-converter?unix={KnownUnixSeconds}");
        await AssertGoldenAsync(unversioned);

        var versioned = await client.GetAsync(
            $"/api/v1.0/tools/timestamp-converter?iso={Uri.EscapeDataString(KnownIso)}");
        await AssertGoldenAsync(versioned);
    }

    private static async Task AssertGoldenAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<TimestampConverterDto>(JsonOptions);
        payload.ShouldNotBeNull();
        payload.Unix.ShouldBe(KnownUnixSeconds);
        payload.Iso.ShouldBe(KnownIso);
        payload.Human.ShouldBe(KnownHuman);
    }

    private sealed class TimestampConverterDto
    {
        public long Unix { get; init; }
        public string? Iso { get; init; }
        public string? Human { get; init; }
    }
}

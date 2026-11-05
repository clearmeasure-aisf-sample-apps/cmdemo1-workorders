using System.Net;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

/// <summary>
/// The acceptance host runs as Development. That file turns BuildingManagement and VendorManagement on,
/// which is the tdd default. The host does not stub <c>IFeatureFlags</c>.
/// Flag-off navigation, deep links, and MCP or API fail-closed behavior belong to the Building and Vendor
/// gates. This host stays on so the existing Buildings and Vendors checks keep passing.
/// Integration tests cover the shared runtime when those flags are configured off.
/// </summary>
[TestFixture]
public class FeatureFlagsApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task GetFeatureFlags_Should_Return200WithJsonDictionary_When_EndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/features/flags");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(doc.RootElement.GetRawText());
        dict.ShouldNotBeNull();
        dict.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GetFeatureFlags_Should_Return200WithJsonDictionary_When_VersionedEndpointCalled()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/v1.0/features/flags");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var response = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(doc.RootElement.GetRawText());
        dict.ShouldNotBeNull();
        dict.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task GetFeatureFlags_Should_EnableBuildingAndVendor_And_MatchDiagnostics_When_LocalHostMatchesTdd()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires the local Development host (tdd ON). This host does not stub IFeatureFlags.");

        var client = TestHttpClientFactory.CreateInsecureClient();
        using var flagsResponse = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/features/flags");
        flagsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var flagsDocument = JsonDocument.Parse(await flagsResponse.Content.ReadAsStringAsync());
        flagsDocument.RootElement.GetProperty("BuildingManagement").GetBoolean().ShouldBeTrue();
        flagsDocument.RootElement.GetProperty("VendorManagement").GetBoolean().ShouldBeTrue();

        using var diagnosticsResponse = await client.GetAsync($"{ServerFixture.ApplicationBaseUrl}/api/diagnostics");
        diagnosticsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var diagnosticsDocument = JsonDocument.Parse(await diagnosticsResponse.Content.ReadAsStringAsync());
        var diagnosticsFlags = diagnosticsDocument.RootElement.GetProperty("featureFlags");
        foreach (var property in flagsDocument.RootElement.EnumerateObject())
        {
            diagnosticsFlags.GetProperty(property.Name).GetBoolean().ShouldBe(property.Value.GetBoolean());
        }
    }
}

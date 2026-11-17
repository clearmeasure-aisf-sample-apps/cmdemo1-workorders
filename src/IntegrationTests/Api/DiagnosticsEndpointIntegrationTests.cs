using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class DiagnosticsEndpointIntegrationTests
{
    private DiagnosticsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DiagnosticsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetDiagnosticsUnversioned()
    {
        var response = await _client!.GetAsync("/api/diagnostics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetDiagnosticsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/diagnostics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeEnvironment_FromHost_When_GetDiagnostics()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Environment.ShouldBe("Testing");
    }

    [Test]
    public async Task Should_ExposeNonNegativeUptime_When_GetDiagnostics()
    {
        var before = DateTime.UtcNow;
        var response = await _client!.GetAsync("/api/diagnostics");
        var after = DateTime.UtcNow;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        var health = SimpleHealthResponseBuilder.Build(TimeProvider.System);
        (after - before).ShouldBeLessThan(TimeSpan.FromSeconds(30));
        payload.Uptime.ShouldBeLessThanOrEqualTo(health.Uptime + TimeSpan.FromSeconds(2));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(health.Uptime - TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromConfiguration_When_GetDiagnostics()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.FeatureFlags[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        payload.FeatureFlags[FeatureFlagNames.VendorManagement].ShouldBeFalse();
        payload.FeatureFlags[FeatureFlagNames.ShowPriorityBadges].ShouldBeTrue();
    }

    [Test]
    public async Task Should_MatchFeatureFlagsEndpoint_When_GetDiagnostics()
    {
        var flags = await ReadFlagMapAsync(_client!, "/api/features/flags");
        var diagnostics = await ReadDiagnosticsFlagsAsync(_client!, "/api/diagnostics");

        diagnostics.ShouldBe(flags);
    }


    private static async Task<Dictionary<string, bool>> ReadFlagMapAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        return payload;
    }

    private static async Task<Dictionary<string, bool>> ReadDiagnosticsFlagsAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        return new Dictionary<string, bool>(payload.FeatureFlags, StringComparer.Ordinal);
    }

    [Test]
    public async Task Should_NotConflictWithLegacyDiagnostics_When_RoutesRegistered()
    {
        var response = await _client!.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var legacyResponse = await _client.PostAsync("/_diagnostics/reset-db-connections", null);
        legacyResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_RejectAnonymousLamar_When_TestingEnvironmentWithoutApiKey(string path)
    {
        var response = await _client!.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_RejectAnonymousReset_When_TestingEnvironmentWithoutApiKey()
    {
        var response = await _client!.PostAsync("/_diagnostics/reset-db-connections", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

}

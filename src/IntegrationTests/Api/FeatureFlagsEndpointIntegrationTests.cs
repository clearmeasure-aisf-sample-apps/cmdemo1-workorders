using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class FeatureFlagsEndpointIntegrationTests
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
    public async Task Should_Return200AndCatalogPayload_When_GetFeatureFlagsUnversioned()
    {
        var response = await _client!.GetAsync("/api/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string? mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await AssertCatalogPayloadAsync(response);
    }

    [Test]
    public async Task Should_Return200AndCatalogPayload_When_GetFeatureFlagsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string? mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await AssertCatalogPayloadAsync(response);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndFeatureFlagsProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/features/flags");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/features/flags");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/features/flags");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/features/flags");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReportSameRuntimeValues_When_FlagsAndDiagnosticsRead()
    {
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["FeatureFlags:VendorManagement"] = "true",
                    ["FeatureFlags:ShowPriorityBadges"] = "false"
                })));
        using var client = factory.CreateClient();

        var flags = await client.GetFromJsonAsync<Dictionary<string, bool>>("/api/features/flags");
        var diagnostics = await client.GetFromJsonAsync<DiagnosticsResponse>(
            "/api/diagnostics", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        flags.ShouldNotBeNull();
        flags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
        flags[FeatureFlagNames.BuildingManagement].ShouldBeFalse();
        flags[FeatureFlagNames.ShowPriorityBadges].ShouldBeFalse();
        diagnostics.ShouldNotBeNull();
        diagnostics.FeatureFlags.ShouldBe(flags, ignoreOrder: true);
    }

    private static async Task AssertCatalogPayloadAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);

        foreach (var (name, enabled) in ExpectedDefaults)
        {
            doc.RootElement.TryGetProperty(name, out var value).ShouldBeTrue($"missing flag '{name}'");
            value.GetBoolean().ShouldBe(enabled, $"flag '{name}'");
        }

        doc.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(FeatureFlagNames.All);
    }

    private static readonly Dictionary<string, bool> ExpectedDefaults = new()
    {
        [FeatureFlagNames.SampleFeatureA] = true,
        [FeatureFlagNames.SampleFeatureB] = false,
        [FeatureFlagNames.ShowPriorityBadges] = true,
        [FeatureFlagNames.BuildingManagement] = false,
        [FeatureFlagNames.VendorManagement] = false
    };
}

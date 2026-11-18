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
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await AssertCatalogPayloadAsync(response);
    }

    [Test]
    public async Task Should_Return200AndCatalogPayload_When_GetFeatureFlagsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/features/flags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await AssertCatalogPayloadAsync(response);
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
        [FeatureFlagNames.ShowPriorityBadges] = true,
        [FeatureFlagNames.BuildingManagement] = true,
        [FeatureFlagNames.VendorManagement] = false
    };
}

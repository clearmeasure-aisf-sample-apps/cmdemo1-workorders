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

/// <summary>
/// API-key-protected and configuration-overridden tests for the diagnostics endpoint.
/// Extracted into their own fixture to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/>
/// being active simultaneously with the shared factory from
/// <see cref="DiagnosticsEndpointIntegrationTests"/>, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class DiagnosticsEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_ReportBuildingAndVendorFromConfiguration_When_Overridden()
    {
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:BuildingManagement"] = "true",
                    ["FeatureFlags:VendorManagement"] = "false"
                });
            });
        });
        using var client = factory.CreateClient();

        var flags = await ReadFlagMapAsync(client, "/api/features/flags");
        var diagnostics = await ReadDiagnosticsFlagsAsync(client, "/api/v1.0/diagnostics");

        flags[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        flags[FeatureFlagNames.VendorManagement].ShouldBeFalse();
        diagnostics[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        diagnostics[FeatureFlagNames.VendorManagement].ShouldBeFalse();
    }

    [Test]
    public async Task Should_ExposeFeatureFlags_FromOverriddenConfiguration_When_GetDiagnostics()
    {
        await using var factory = new DiagnosticsWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:BuildingManagement"] = "false",
                    ["FeatureFlags:VendorManagement"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/diagnostics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload.FeatureFlags[FeatureFlagNames.BuildingManagement].ShouldBeFalse();
        payload.FeatureFlags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
    }

    [TestCase("/_lamar/services")]
    [TestCase("/_lamar/scanning")]
    public async Task Should_AllowAuthorizedLamar_When_ApiKeyEnabled(string path)
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_AllowAuthorizedReset_When_ApiKeyEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.PostAsync("/_diagnostics/reset-db-connections", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndDiagnosticsProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/diagnostics");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/diagnostics");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/diagnostics");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/diagnostics");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
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
}

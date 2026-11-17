using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// API-key-protected tests for <c>GET /api/metrics/summary</c>. Extracted into their own
/// fixture to avoid a second <see cref="WebApplicationFactory{TEntryMarker}"/> being active
/// simultaneously with the shared factory from
/// <see cref="MetricsSummaryEndpointIntegrationTests"/>, which races on the global
/// <c>HostFactoryResolver</c>.
/// </summary>
[TestFixture]
public class MetricsSummaryEndpointIsolatedFactoryIntegrationTests
{
    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndMetricsProtected()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/metrics/summary");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/metrics/summary");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/metrics/summary");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

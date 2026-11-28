using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
[Explicit("Starts full app")]
public class HotPathOutputCacheEndpointTests
{
    [Test]
    public async Task Should_ServePingFromOutputCache_When_SecondRequestWithinTtl()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/ping");
        using var second = await client.GetAsync("/api/ping");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_NotCacheDetailedHealthCheck_When_RequestedTwice()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/_healthcheck/detailed");
        using var second = await client.GetAsync("/_healthcheck/detailed");

        first.Headers.Age.ShouldBeNull();
        second.Headers.Age.ShouldBeNull();
    }
}

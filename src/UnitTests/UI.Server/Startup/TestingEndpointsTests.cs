using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Startup;

[TestFixture]
public class TestingEndpointsTests
{
    [Test]
    public async Task ShouldServeTheCompressionProbe_WhenTheHostRunsInTesting()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/_test/compression-probe");

        body.ShouldBe(new string('A', 4096));
    }

    [Test]
    public async Task ShouldCountEachCall_WhenTheIdempotencyProbeIsPosted()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsync("/api/_test/idempotency-probe", null);
        var second = await client.PostAsync("/api/_test/idempotency-probe", null);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await first.Content.ReadAsStringAsync()).ShouldBe("count=1");
        (await second.Content.ReadAsStringAsync()).ShouldBe("count=2");
    }

    [Test]
    public async Task ShouldEchoTheBody_WhenTheEchoProbeIsPosted()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/__test/request-body-echo", new StringContent("hello"));

        (await response.Content.ReadAsStringAsync()).ShouldBe("hello");
    }
}

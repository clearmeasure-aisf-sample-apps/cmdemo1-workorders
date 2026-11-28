using System.Net;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
[Explicit("Starts full app")]
public class PathBasedRateLimitWebTests
{
    private static IReadOnlyDictionary<string, string?> TwoPermitSettings() =>
        new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = ApiKeyConstants.HeaderName
        };

    [TestCase("/api/blazor-wasm-single-api")]
    [TestCase("/api/v1.0/blazor-wasm-single-api")]
    [TestCase("/mcp")]
    public async Task Should_Return429_When_RequestsExceedPermitLimitOnPathGatedRoute(string path)
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(TwoPermitSettings());
        using var client = factory.CreateClient();

        var first = await client.GetAsync(path);
        await client.GetAsync(path);
        var limited = await client.GetAsync(path);

        first.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        limit.First().ShouldBe("2");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.TryGetValues("Retry-After", out var retryAfter).ShouldBeTrue();
        retryAfter.First().ShouldBe("60");
    }
}

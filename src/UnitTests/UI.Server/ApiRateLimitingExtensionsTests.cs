using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiRateLimitingExtensionsTests
{
    [Test]
    public void Should_RejectSecondApiCall_When_PermitLimitIsOne()
    {
        var limiter = CreateLimiter("true");
        var context = CreateContext("/api/work-orders");

        using var first = limiter.AttemptAcquire(context);
        using var second = limiter.AttemptAcquire(context);

        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeFalse();
    }

    [TestCase("true", "/index.html")]
    [TestCase("false", "/api/work-orders")]
    public void Should_AllowEveryCall_When_PathIsNotApiOrLimitingDisabled(string enabled, string path)
    {
        var limiter = CreateLimiter(enabled);
        var context = CreateContext(path);

        using var first = limiter.AttemptAcquire(context);
        using var second = limiter.AttemptAcquire(context);

        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeTrue();
    }

    private static PartitionedRateLimiter<HttpContext> CreateLimiter(string enabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ApiRateLimitingOptions.SectionName}:Enabled"] = enabled,
                [$"{ApiRateLimitingOptions.SectionName}:PermitLimit"] = "1",
                [$"{ApiRateLimitingOptions.SectionName}:WindowSeconds"] = "60",
                [$"{ApiRateLimitingOptions.SectionName}:SegmentsPerWindow"] = "1"
            })
            .Build();
        return new ServiceCollection()
            .AddApiRateLimiting(configuration)
            .BuildServiceProvider()
            .GetRequiredService<PartitionedRateLimiter<HttpContext>>();
    }

    private static DefaultHttpContext CreateContext(string path) => new() { Request = { Path = path } };
}

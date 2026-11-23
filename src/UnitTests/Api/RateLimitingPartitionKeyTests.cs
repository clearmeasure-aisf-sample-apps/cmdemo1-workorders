using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class RateLimitingPartitionKeyTests
{
    [Test]
    public void Should_UseApiKeyPartition_When_HeaderPresent()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[ApiKeyConstants.HeaderName] = "secret-1";
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, ApiKeyConstants.HeaderName).ShouldBe("key:secret-1");
    }

    [Test]
    public void Should_UseIpPartition_When_NoApiKey()
    {
        var ctx = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5") }
        };
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, ApiKeyConstants.HeaderName).ShouldBe("ip:10.0.0.5");
    }

    [Test]
    public void Should_UseAnonymousPartition_When_NoIpAndNoKey()
    {
        var ctx = new DefaultHttpContext();
        ApiRateLimitingExtensions.ResolvePartitionKey(ctx, ApiKeyConstants.HeaderName).ShouldBe("anonymous");
    }
}

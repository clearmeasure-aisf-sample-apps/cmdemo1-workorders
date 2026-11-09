using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;
using HttpEndpoint = Microsoft.AspNetCore.Http.Endpoint;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class RateLimitingMiddlewareRulesTests
{
    [Test]
    public void ShouldApply_WhenTheEndpointUsesTheApiPolicy()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName));

        var applies = RateLimitingMiddlewareRules.ShouldApply(context);

        applies.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotApply_WhenTheEndpointUsesAnotherPolicy()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute("other"));

        var applies = RateLimitingMiddlewareRules.ShouldApply(context);

        applies.ShouldBeFalse();
    }

    private static DefaultHttpContext ContextWithEndpoint(EnableRateLimitingAttribute attribute)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new HttpEndpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(attribute), "probe"));
        return context;
    }
}

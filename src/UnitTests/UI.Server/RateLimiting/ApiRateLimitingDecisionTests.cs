using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;
using HttpEndpoint = Microsoft.AspNetCore.Http.Endpoint;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class ApiRateLimitingDecisionTests
{
    private const string ApiPath = "/api/work-orders";

    [Test]
    public void ShouldApply_WhenTheEndpointUsesTheApiPolicy()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName));

        var applies = ApiRateLimitingDecision.EndpointUsesApiPolicy(context);

        applies.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotApply_WhenTheEndpointUsesAnotherPolicy()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute("other"));

        var applies = ApiRateLimitingDecision.EndpointUsesApiPolicy(context);

        applies.ShouldBeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void IsEnabled_ReflectsTheOption_WhenSet(bool enabled)
    {
        ApiRateLimitingDecision.IsEnabled(new ApiRateLimitingOptions { Enabled = enabled }).ShouldBe(enabled);
    }

    [TestCase(ApiPath, true)]
    [TestCase("/index.html", false)]
    public void IsRateLimitedPath_ReturnsExpected_WhenPathGiven(string path, bool expected)
    {
        var context = new DefaultHttpContext { Request = { Path = path } };

        ApiRateLimitingDecision.IsRateLimitedPath(context).ShouldBe(expected);
    }

    [Test]
    public void AppliesTo_ReturnsTrue_WhenEveryTermHolds()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName), ApiPath);

        ApiRateLimitingDecision.AppliesTo(context, new ApiRateLimitingOptions { Enabled = true }).ShouldBeTrue();
    }

    [Test]
    public void AppliesTo_ReturnsFalse_WhenOnlyTheMetadataTermFails()
    {
        var context = new DefaultHttpContext { Request = { Path = ApiPath } };
        var options = new ApiRateLimitingOptions { Enabled = true };

        ApiRateLimitingDecision.LimiterAppliesTo(context, options).ShouldBeTrue();
        ApiRateLimitingDecision.AppliesTo(context, options).ShouldBeFalse();
    }

    [Test]
    public void AppliesTo_ReturnsFalse_WhenOnlyTheEnabledTermFails()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName), ApiPath);
        var options = new ApiRateLimitingOptions { Enabled = false };

        ApiRateLimitingDecision.LimiterAppliesTo(context, options).ShouldBeFalse();
        ApiRateLimitingDecision.AppliesTo(context, options).ShouldBeFalse();
    }

    [Test]
    public void AppliesTo_ReturnsFalse_WhenOnlyThePathTermFails()
    {
        var context = ContextWithEndpoint(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName), "/index.html");
        var options = new ApiRateLimitingOptions { Enabled = true };

        ApiRateLimitingDecision.LimiterAppliesTo(context, options).ShouldBeFalse();
        ApiRateLimitingDecision.AppliesTo(context, options).ShouldBeFalse();
    }

    private static DefaultHttpContext ContextWithEndpoint(EnableRateLimitingAttribute attribute, string path = "")
    {
        var context = new DefaultHttpContext { Request = { Path = path } };
        context.SetEndpoint(new HttpEndpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(attribute), "probe"));
        return context;
    }
}

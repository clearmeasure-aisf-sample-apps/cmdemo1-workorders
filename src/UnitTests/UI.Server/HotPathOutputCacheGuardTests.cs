using System.Net.Http.Headers;
using System.Reflection;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HotPathOutputCacheGuardTests
{
    private int _invocations;

    [SetUp]
    public void SetUp() => _invocations = 0;

    [Test]
    public void HotPathTimeToLive_WhenRead_ShouldBeShortButPositive()
    {
        HotPathOutputCacheGuard.HotPathTimeToLive.ShouldBeGreaterThan(TimeSpan.Zero);
        HotPathOutputCacheGuard.HotPathTimeToLive.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
    }

    [TestCase(OutputCachePolicyNames.Ping)]
    [TestCase(OutputCachePolicyNames.SimpleHealth)]
    [TestCase(OutputCachePolicyNames.VersionMetadata)]
    public async Task Get_WhenSecondRequestWithinTtl_ShouldServeCachedResponse(string policyName)
    {
        await using var app = await StartAppAsync(endpoint => endpoint.CacheOutput(policyName));
        using var client = app.GetTestClient();

        var first = await client.GetStringAsync("/probe");
        var second = await client.GetStringAsync("/probe");

        second.ShouldBe(first);
        _invocations.ShouldBe(1);
    }

    [Test]
    public async Task Get_WhenEndpointHasNoNamedPolicy_ShouldNotCache()
    {
        await using var app = await StartAppAsync(_ => { });
        using var client = app.GetTestClient();

        await client.GetStringAsync("/probe");
        await client.GetStringAsync("/probe");

        _invocations.ShouldBe(2);
    }

    [Test]
    public async Task Get_WhenRequestIsAuthenticated_ShouldNotCache()
    {
        await using var app = await StartAppAsync(endpoint => endpoint.CacheOutput(OutputCachePolicyNames.Ping));
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token");

        await client.GetStringAsync("/probe");
        await client.GetStringAsync("/probe");

        _invocations.ShouldBe(2);
    }

    [Test]
    public async Task Post_WhenEndpointHasHotPathPolicy_ShouldNotCache()
    {
        await using var app = await StartAppAsync(endpoint => endpoint.CacheOutput(OutputCachePolicyNames.Ping));
        using var client = app.GetTestClient();

        (await client.PostAsync("/probe", null)).Dispose();
        (await client.PostAsync("/probe", null)).Dispose();

        _invocations.ShouldBe(2);
    }

    [Test]
    public async Task Get_WhenNeedsRebootHeaderDiffers_ShouldNotShareSimpleHealthEntry()
    {
        await using var app = await StartAppAsync(endpoint => endpoint.CacheOutput(OutputCachePolicyNames.SimpleHealth));
        using var client = app.GetTestClient();

        await client.GetStringAsync("/probe");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request.Headers.TryAddWithoutValidation(TestSupportConstants.NeedsRebootHeaderName, "true");
        (await client.SendAsync(request)).Dispose();

        _invocations.ShouldBe(2);
    }

    [Test]
    public void PingController_WhenInspected_ShouldUsePingPolicy()
    {
        var attribute = typeof(PingController).GetMethod(nameof(PingController.Get))!
            .GetCustomAttribute<OutputCacheAttribute>();

        attribute.ShouldNotBeNull();
        attribute.PolicyName.ShouldBe(OutputCachePolicyNames.Ping);
    }

    [TestCase(typeof(MetricsController))]
    [TestCase(typeof(WorkOrderStatusCountsController))]
    public void Controller_WhenServingLiveCounters_ShouldNotBeOutputCached(Type controller)
    {
        var cachedActions = controller.GetMethods()
            .Where(method => method.GetCustomAttribute<OutputCacheAttribute>() is not null);

        controller.GetCustomAttribute<OutputCacheAttribute>().ShouldBeNull();
        cachedActions.ShouldBeEmpty();
    }

    private async Task<WebApplication> StartAppAsync(Action<IEndpointConventionBuilder> applyCachePolicy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOutputCache(HotPathOutputCacheGuard.AddPolicies);
        var app = builder.Build();
        app.UseOutputCache();
        applyCachePolicy(app.MapMethods("/probe", [HttpMethods.Get, HttpMethods.Post], Probe));
        await app.StartAsync();
        return app;
    }

    private string Probe() => $"call {Interlocked.Increment(ref _invocations)}";
}

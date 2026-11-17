using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>The API group gets the request-timeout policy and the CORS policy only when each is switched on.</summary>
[TestFixture]
public class ApiEndpointConventionsTests
{
    [Test]
    public void ApplyRequestTimeout_Should_AddThePolicy_When_TimeoutsAreEnabled()
    {
        var endpoint = MapApiPing(timeoutsEnabled: true, new ServerCorsOptions());

        endpoint.Metadata.GetMetadata<RequestTimeoutAttribute>().ShouldNotBeNull().PolicyName
            .ShouldBe(ApiRequestTimeoutsExtensions.ApiControllersPolicyName);
    }

    [Test]
    public void ApplyRequestTimeout_Should_LeaveThePolicyOff_When_TimeoutsAreDisabled()
    {
        var endpoint = MapApiPing(timeoutsEnabled: false, new ServerCorsOptions());

        endpoint.Metadata.GetMetadata<RequestTimeoutAttribute>().ShouldBeNull();
    }

    [Test]
    public void ApplyCorsWhenActive_Should_LeaveCorsOff_When_NoOriginIsAllowed()
    {
        var endpoint = MapApiPing(timeoutsEnabled: false, new ServerCorsOptions { Enabled = true });

        endpoint.Metadata.GetMetadata<ICorsMetadata>().ShouldBeNull();
    }

    [Test]
    public void ApplyCorsWhenActive_Should_RequireTheServerPolicy_When_EnabledWithAnOrigin()
    {
        var endpoint = MapApiPing(timeoutsEnabled: false,
            new ServerCorsOptions { Enabled = true, AllowedOrigins = ["https://church.example"] });

        endpoint.Metadata.GetMetadata<IEnableCorsAttribute>().ShouldNotBeNull().PolicyName.ShouldBe(ServerCorsOptions.PolicyName);
    }

    private static RouteEndpoint MapApiPing(bool timeoutsEnabled, ServerCorsOptions cors)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.Configure<ApiRequestTimeoutOptions>(timeouts => timeouts.Enabled = timeoutsEnabled);
        builder.Services.AddSingleton(Options.Create(cors));
        var app = builder.Build();
        var api = app.MapGroup("api");
        ApiEndpointConventions.ApplyRequestTimeout(app, api);
        ApiEndpointConventions.ApplyCorsWhenActive(app, api);
        api.MapGet("ping", () => "pong");
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Single();
    }
}

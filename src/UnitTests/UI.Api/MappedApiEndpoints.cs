using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary>The endpoints <see cref="ApiEndpoints"/> maps, read from a host that only maps the API.</summary>
internal static class MappedApiEndpoints
{
    private static readonly Lazy<IReadOnlyList<RouteEndpoint>> All = new(Map);

    /// <summary>Endpoints whose route is <paramref name="route"/> (no leading slash).</summary>
    internal static IReadOnlyList<RouteEndpoint> At(string route) =>
        All.Value.Where(endpoint => Route(endpoint) == route).ToList();

    /// <summary>
    /// <paramref name="route"/> (under <c>api/</c>) answers <paramref name="method"/> anonymously at the plain and the
    /// versioned prefix, under the API sliding-window rate limit.
    /// </summary>
    internal static void ShouldBeAnonymousAndRateLimited(string route, string method)
    {
        foreach (var prefix in new[] { "api", ApiRoutes.VersionedApiPrefix })
        {
            var endpoint = At($"{prefix}/{route}").ShouldHaveSingleItem();
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.ShouldBe([method]);
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
            endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.ShouldBe(ApiRateLimiting.PolicyName);
        }
    }

    /// <summary>The services endpoints take as parameters, so mapping binds them from DI rather than the body.</summary>
    private static void AddEndpointDependencies(IServiceCollection services)
    {
        services.AddSingleton<IBus>(_ => throw new NotSupportedException());
        services.AddSingleton<IDatabaseConfiguration>(_ => throw new NotSupportedException());
        services.AddSingleton<IDetailedHealthReportProvider>(_ => throw new NotSupportedException());
        services.AddSingleton<IHttpRequestMetricsCounter>(_ => throw new NotSupportedException());
        services.AddSingleton<IWorkOrderNumberGenerator>(_ => throw new NotSupportedException());
        services.AddSingleton(new RegisteredServices(services));
        services.AddSingleton(TimeProvider.System);
    }

    private static string Route(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText!.TrimStart('/');

    private static IReadOnlyList<RouteEndpoint> Map()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddApiVersioning(options => options.DefaultApiVersion = new ApiVersion(1, 0));
        builder.Services.AddOutputCache();
        AddEndpointDependencies(builder.Services);
        var app = builder.Build();
        app.MapGroup("").MapApiEndpoints();
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }
}

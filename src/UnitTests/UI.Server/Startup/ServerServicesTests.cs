using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Server.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Startup;

[TestFixture]
public class ServerServicesTests
{
    [Test]
    public async Task ShouldRegisterTheRequestProtections_WhenTheHostStarts()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();

        var services = factory.Services;

        services.GetService<IdempotencyProbeState>().ShouldNotBeNull();
        services.GetRequiredService<IOptions<IdempotencyOptions>>().Value.ShouldNotBeNull();
        services.GetService<IOutputCacheStore>().ShouldNotBeNull();
        services.GetService<TimeProvider>().ShouldBe(TimeProvider.System);
    }
}

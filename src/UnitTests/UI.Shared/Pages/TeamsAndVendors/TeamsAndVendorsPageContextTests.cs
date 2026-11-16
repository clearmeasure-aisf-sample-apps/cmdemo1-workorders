using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

/// <summary>
/// Verifies DI registration for <see cref="TeamsAndVendorsPageContext"/>.
/// </summary>
public class TeamsAndVendorsPageContextTests
{
    [Test]
    public void Build_RegistersBusFeatureFlagsUiBusAuthAndClock()
    {
        var page = TeamsAndVendorsPageContext.Create();

        using var ctx = page.Build();

        ctx.Services.GetRequiredService<IBus>().ShouldBeOfType<TeamsAndVendorsBus>();
        ctx.Services.GetRequiredService<IFeatureFlags>().ShouldBeOfType<StubFeatureFlags>();
        ctx.Services.GetRequiredService<IUiBus>().ShouldBeOfType<StubUiBus>();
        ctx.Services.GetRequiredService<AuthenticationStateProvider>().ShouldNotBeNull();
        ctx.Services.GetRequiredService<TimeProvider>().ShouldBe(TimeProvider.System);
        ReferenceEquals(page.Bus, ctx.Services.GetRequiredService<IBus>()).ShouldBeTrue();
    }

    [Test]
    public void WithBus_AndWithFeatureFlags_RegistersProvidedInstances()
    {
        var bus = new TeamsAndVendorsBus();
        var flags = new StubFeatureFlags(vendorManagement: false);
        var page = TeamsAndVendorsPageContext.Create()
            .WithBus(bus)
            .WithFeatureFlags(flags);

        using var ctx = page.Build();

        ReferenceEquals(ctx.Services.GetRequiredService<IBus>(), bus).ShouldBeTrue();
        ReferenceEquals(ctx.Services.GetRequiredService<IFeatureFlags>(), flags).ShouldBeTrue();
    }
}

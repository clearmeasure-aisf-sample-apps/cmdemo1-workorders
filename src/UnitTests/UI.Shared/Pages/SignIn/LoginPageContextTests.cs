using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

public class LoginPageContextTests
{
    [Test]
    public void Build_RegistersBusAuthEnvironmentAndSession()
    {
        var page = LoginPageContext.Create();

        using var ctx = page.Build();

        ctx.Services.GetRequiredService<IBus>().ShouldBeOfType<StubBus>();
        ctx.Services.GetRequiredService<AuthenticationStateProvider>()
            .ShouldBeOfType<CustomAuthenticationStateProvider>();
        ReferenceEquals(ctx.Services.GetRequiredService<CustomAuthenticationStateProvider>(), page.Auth)
            .ShouldBeTrue();
        ctx.Services.GetRequiredService<IUserSession>().ShouldBeOfType<StubUserSession>();
        ctx.Services.GetRequiredService<IUiBus>().ShouldBeOfType<StubUiBus>();
        ctx.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.ShouldBe("Testing");
    }

    [Test]
    public void WithBus_UsesProvidedBus()
    {
        var bus = new LoginFailingEmployeeBus();
        var page = LoginPageContext.Create().WithBus(bus);

        using var ctx = page.Build();

        ReferenceEquals(ctx.Services.GetRequiredService<IBus>(), bus).ShouldBeTrue();
    }

    [Test]
    public void WithEnvironment_RegistersHostEnvironmentName()
    {
        var page = LoginPageContext.Create().WithEnvironment("Staging");

        using var ctx = page.Build();

        ctx.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.ShouldBe("Staging");
    }
}

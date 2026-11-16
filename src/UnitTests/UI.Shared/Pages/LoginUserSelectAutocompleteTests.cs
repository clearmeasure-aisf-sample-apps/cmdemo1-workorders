using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class LoginUserSelectAutocompleteTests
{
    [Test]
    public async Task ShouldTurnOffAutofillOnTheMemberDropdown()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<Login>();

        component.Find($"[data-testid='{Login.Elements.User}']").GetAttribute("autocomplete").ShouldBe("off");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        var auth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton<AuthenticationStateProvider>(auth);
        ctx.Services.AddSingleton(auth);
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession { OnSignIn = auth.Login });
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IHostEnvironment>(
            new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Testing" });
        return ctx;
    }
}

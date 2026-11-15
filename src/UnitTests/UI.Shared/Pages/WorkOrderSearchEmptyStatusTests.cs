using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchEmptyStatusTests
{
    private static BunitContext CreateContext()
    {
        const string username = "jpalermo";
        var context = new BunitContext { JSInterop = { Mode = JSRuntimeMode.Loose } };
        context.Services.AddLogging();

        var bus = new StubBus([]);
        var uiBus = new StubUiBus();
        var sessionStore = new StubUserSessionStore { Username = username };
        var authentication = new CustomAuthenticationStateProvider(sessionStore);
        authentication.Login(username).GetAwaiter().GetResult();

        context.Services.AddSingleton<AuthenticationStateProvider>(authentication);
        context.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        context.Services.AddSingleton<IUserSession>(new StubUserSession());
        context.Services.AddSingleton<WorkOrderSearchState>();
        context.Services.AddSingleton<IUiBus>(uiBus);
        context.Services.AddSingleton<IBus>(bus);
        context.Services.AddSingleton(new WorkOrderBulkActionService(bus, uiBus));
        context.Services.AddSingleton(TimeProvider.System);

        return context;
    }

    [Test]
    public async Task EmptyResults_AreAnnouncedWithRoleStatus()
    {
        await using var context = CreateContext();

        var component = context.Render<WorkOrderSearch>();

        component.Find(".empty-results").GetAttribute("role").ShouldBe("status");
    }
}

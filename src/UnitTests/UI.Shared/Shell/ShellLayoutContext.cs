using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

/// <summary>
/// Builds a bUnit context for app-shell components (MainLayout and extracted chrome).
/// </summary>
internal static class ShellLayoutContext
{
    internal static BunitContext Create(
        IBus? bus = null,
        string? authenticateAsUser = null,
        StubUserSession? userSession = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bunitAuth = ctx.AddAuthorization();
        if (authenticateAsUser != null)
            bunitAuth.SetAuthorized(authenticateAsUser);

        ctx.Services.AddLogging();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(bus ?? new StubBus());
        ctx.Services.AddSingleton<IUserSession>(userSession ?? new StubUserSession(userName: authenticateAsUser));
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(new ChurchCalendar(TimeProvider.System));
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        if (authenticateAsUser != null)
            customAuth.Login(authenticateAsUser).GetAwaiter().GetResult();

        ctx.Services.AddSingleton(customAuth);
        return ctx;
    }

    internal static StubBus BusWithDeployment(DeploymentIdentity deployment)
    {
        return new StubBus { Deployment = deployment };
    }

    internal static StubBus BusThatFailsDeployment()
    {
        return new FailingDeploymentBus();
    }

    internal static StubBus BusThatFailsDeploymentWithRemotingError()
    {
        return new RemotingFailureDeploymentBus();
    }

    private sealed class FailingDeploymentBus : StubBus
    {
        public override Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request)
        {
            if (request is DeploymentIdentityQuery)
                throw new HttpRequestException("boom");
            return base.Send(request);
        }
    }

    private sealed class RemotingFailureDeploymentBus : StubBus
    {
        public override Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request)
        {
            if (request is DeploymentIdentityQuery)
                throw new InvalidOperationException("Remoting call failed (500)");
            return base.Send(request);
        }
    }
}

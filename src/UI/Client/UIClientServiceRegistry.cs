using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Mediator;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Client;

/// <summary>
/// Every service the WebAssembly client resolves, named explicitly on Microsoft DI (no convention scanning).
/// </summary>
// ReSharper disable once InconsistentNaming -- UI is the product abbreviation in this type name
public static class UIClientServiceRegistry
{
    /// <summary>Registers authentication, the remotable bus, client services and the client health checks.</summary>
    public static IServiceCollection AddUiClientServices(this IServiceCollection services)
    {
        AddAuthentication(services);
        AddRemotableBus(services);
        AddClientServices(services);
        services.AddHealthChecks()
            .AddCheck<HealthCheckTracer>("UI.Client")
            .AddCheck<RemotableBusHealthCheck>("Remotable Bus")
            .AddCheck<ServerHealthCheck>("Server health check");
        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddSingleton<IUserSessionStore, LocalStorageUserSessionStore>();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthenticationStateProvider>());
    }

    /// <summary>Requests go to the server through the remotable bus; UI events through the MVC bus.</summary>
    private static void AddRemotableBus(IServiceCollection services)
    {
        services.AddScoped<IUiBus>(_ => new MvcBus(NullLogger<MvcBus>.Instance));
        services.AddScoped<IUserSession, UserSession>();
        // In-browser handlers (the LlmGateway chat handlers); remotable requests skip the mediator and go to the server.
        services.AddMediator((MediatorOptions options) =>
        {
            // Outside the layer namespaces: the generated Mediator and its Internals namespace reference each other, which the namespace-cycle architecture rule would flag.
            options.Namespace = "ClearMeasure.Bootcamp.Generated.UiClientMediator";
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = [typeof(IRemotableRequest), typeof(UIClientServiceRegistry), typeof(CanConnectToLlmServerHealthCheck)];
        });
        RemoveServerOnlyHandlers(services);
        services.AddTransient<IPublisherGateway>(sp =>
            new PublisherGateway(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<IConfiguration>()));
        services.AddTransient<IBus, RemotableBus>();
    }

    /// <summary>
    /// <see cref="ApplicationChatQuery"/> is remotable, so <see cref="RemotableBus"/> always sends it to the server. The
    /// generator still registers its in-process handler from LlmGateway, which needs the server's MCP tool provider;
    /// it is removed so the browser container holds only handlers it can build.
    /// </summary>
    private static void RemoveServerOnlyHandlers(IServiceCollection services)
    {
        services.RemoveAll<IRequestHandler<ApplicationChatQuery, ApplicationChatResult>>();
        services.RemoveAll<ApplicationChatHandler>();
    }

    private static void AddClientServices(IServiceCollection services)
    {
        // Scoped: in the browser one scope lives as long as the app, and the factory uses the scoped bus.
        services.AddScoped<ChatClientFactory>();
        services.AddTransient<WorkOrderTool>();
        services.AddTransient<ITranslationService, TranslationService>();
        services.AddSingleton<ILlmHealthCheckCache>(_ => new LlmHealthCheckCache());
        services.AddTransient<IWorkOrderBuilder, WorkOrderBuilder>();
        services.AddTransient<IWorkOrderNumberGenerator, WorkOrderNumberGenerator>();
        services.AddSingleton<ThemePreferenceService>();
        services.AddSingleton<WorkOrderSearchState>();
        services.AddScoped<IFeatureFlags, BusFeatureFlags>();
        services.AddTransient<WorkOrderBulkActionService>();
        services.AddSingleton(TimeProvider.System);
    }
}

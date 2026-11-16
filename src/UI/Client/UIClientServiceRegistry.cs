using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Lamar;
using Mediator;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Client;

// ReSharper disable once InconsistentNaming -- UI is the product abbreviation in this type name
public class UIClientServiceRegistry : ServiceRegistry
{
    public UIClientServiceRegistry()
    {
        AddAuthentication();
        AddRemotableBus();
        AddClientServices();
        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<UIClientServiceRegistry>();
            scanner.AssemblyContainingType<IRemotableRequest>();
            scanner.AssemblyContainingType<CanConnectToLlmServerHealthCheck>();
        });
        this.AddHealthChecks().AddCheck<HealthCheckTracer>("UI.Client");
        this.AddHealthChecks().AddCheck<RemotableBusHealthCheck>("Remotable Bus");
        this.AddHealthChecks().AddCheck<ServerHealthCheck>("Server health check");
    }

    private void AddAuthentication()
    {
        this.AddSingleton<IUserSessionStore, LocalStorageUserSessionStore>();
        this.AddScoped<CustomAuthenticationStateProvider>();
        this.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthenticationStateProvider>());
    }

    /// <summary>Requests go to the server through the remotable bus; UI events through the MVC bus.</summary>
    private void AddRemotableBus()
    {
        this.AddScoped<IUiBus>(_ => new MvcBus(NullLogger<MvcBus>.Instance));
        this.AddScoped<IUserSession, UserSession>();
        // In-browser handlers (the LlmGateway chat handlers); remotable requests skip the mediator and go to the server.
        this.AddMediator((MediatorOptions options) =>
        {
            // Outside the layer namespaces: the generated Mediator and its Internals namespace reference each other, which the namespace-cycle architecture rule would flag.
            options.Namespace = "ClearMeasure.Bootcamp.Generated.UiClientMediator";
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = [typeof(IRemotableRequest), typeof(UIClientServiceRegistry), typeof(CanConnectToLlmServerHealthCheck)];
        });
        this.AddTransient<IPublisherGateway>(sp =>
            new PublisherGateway(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<IConfiguration>()));
        this.AddTransient<IBus, RemotableBus>();
    }

    private void AddClientServices()
    {
        this.AddSingleton<ChatClientFactory>();
        this.AddTransient<WorkOrderTool>();
        this.AddSingleton<ThemePreferenceService>();
        this.AddSingleton<WorkOrderSearchState>();
        this.AddScoped<IFeatureFlags, BusFeatureFlags>();
        this.AddTransient<WorkOrderBulkActionService>();
        this.AddSingleton(TimeProvider.System);
    }
}
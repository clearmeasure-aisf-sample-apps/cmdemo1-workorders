using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using ClearMeasure.Bootcamp.UI.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FunJeffreyCustomEventHealthCheck = ClearMeasure.Bootcamp.UI.Shared.FunJeffreyCustomEventHealthCheck;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Every application service UI.Server resolves, named explicitly on Microsoft DI (no convention scanning), so the
/// wiring reads top to bottom and works under Native AOT.
/// </summary>
public static class UiServiceRegistry
{
    /// <summary>Registers data access, the realtime bus, the LLM services, validators and health reporting.</summary>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<IStartupFilter, TestingDatabaseStartupFilter>();
        services.AddRequestValidators();
        services.AddSingleton<Middleware.HttpEnvelopeValidatorSet>();
        AddDataAccess(services);
        AddRealtimeBus(services);
        AddApplicationServices(services);
        AddHealthReporting(services);
        return services;
    }

    private static void AddDataAccess(IServiceCollection services)
    {
        services.AddScoped<DbContext, DataContext>();
        // Handlers that take the concrete DataContext get their own instance.
        services.AddTransient<DataContext>();
        services.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
        services.AddScoped<DatabaseMigrationVersionReader>();
    }

    /// <summary>
    /// The source-generated mediator with validation and feature-flag behaviors, behind an <see cref="IBus"/> that also
    /// notifies realtime clients. The generator registers every handler in the referenced assemblies exactly once.
    /// </summary>
    private static void AddRealtimeBus(IServiceCollection services)
    {
        services.AddSingleton<RealtimeNotificationHub>();
        services.AddSingleton<IRealtimeNotificationHub>(sp => sp.GetRequiredService<RealtimeNotificationHub>());
        // Behaviors run in array order, outermost first. Transient matches the old MediatR lifetimes (handlers take DbContext).
        services.AddMediator((MediatorOptions options) =>
        {
            // Outside the layer namespaces: the generated Mediator and its Internals namespace reference each other, which the namespace-cycle architecture rule would flag.
            options.Namespace = "ClearMeasure.Bootcamp.Generated.UiServerMediator";
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>), typeof(FeatureFlagBehavior<,>)];
        });
        services.AddSingleton<IFeatureFlagEvaluator, RuntimeFeatureFlagEvaluator>();
        services.AddTransient<IBus>(provider => new ServerRealtimeBus(
            provider.GetRequiredService<IMediator>(),
            provider.GetRequiredService<IRealtimeNotificationHub>()));
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddTransient<IWorkOrderNumberGenerator, WorkOrderNumberGenerator>();
        services.AddTransient<ChatClientFactory>();
        services.AddTransient<WorkOrderTool>();
        services.AddTransient<ITranslationService, TranslationService>();
        services.AddTransient<IToolProvider, ToolProvider>();
        services.AddTransient<WorkOrderReformatAgent>();
        services.AddSingleton<IOptions<AutoReformatOptions>>(sp =>
            Options.Create(new AutoReformatOptions(sp.GetRequiredService<IConfiguration>())));
        services.AddHostedService<AutoReformatAgentService>();
    }

    private static void AddHealthReporting(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
            .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
            .AddCheck<Is64BitProcessHealthCheck>("Server")
            .AddCheck<HealthCheck>("API")
            .AddCheck<FunJeffreyCustomEventHealthCheck>("Jeffrey")
            .AddCheck<NeedsRebootHealthCheck>("NeedsReboot")
            .AddCheck<ProcessThreadCountHealthCheck>("ProcessThreadCount");
        services.AddSingleton<ILlmHealthCheckCache>(_ => new LlmHealthCheckCache());
        services.AddSingleton<IDetailedHealthReportProvider, DetailedHealthReportProvider>();
        services.AddSingleton<IHttpRequestMetricsCounter, HttpRequestMetricsCounter>();
    }
}

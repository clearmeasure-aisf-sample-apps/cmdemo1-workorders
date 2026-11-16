using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using Lamar;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FunJeffreyCustomEventHealthCheck = ClearMeasure.Bootcamp.UI.Shared.FunJeffreyCustomEventHealthCheck;

namespace ClearMeasure.Bootcamp.UI.Server;

public class UiServiceRegistry : ServiceRegistry
{
    public UiServiceRegistry()
    {
        this.AddSingleton<IStartupFilter, TestingDatabaseStartupFilter>();
        this.AddValidatorsFromAssemblyContaining<WebServiceMessageValidator>(
            filter: result => !HttpEnvelopeValidationDeferral.IsDeferredValidator(result.ValidatorType));
        this.AddSingleton<Middleware.HttpEnvelopeValidatorSet>();
        this.AddScoped<DbContext, DataContext>();
        AddRealtimeBus();
        this.AddTransient<IWorkOrderNumberGenerator, WorkOrderNumberGenerator>();
        this.AddTransient<WorkOrderReformatAgent>();
        this.AddSingleton<IOptions<AutoReformatOptions>>(sp =>
            Options.Create(new AutoReformatOptions(sp.GetRequiredService<IConfiguration>())));
        this.AddHostedService<AutoReformatAgentService>();
        ScanForHandlersAndHealthChecks();
        this.AddScoped<DatabaseMigrationVersionReader>();
        AddHealthReporting();
    }

    /// <summary>
    /// The source-generated mediator with validation and feature-flag behaviors, behind an <see cref="IBus"/> that also
    /// notifies realtime clients. The generator registers every handler in the referenced assemblies exactly once.
    /// </summary>
    private void AddRealtimeBus()
    {
        this.AddSingleton<RealtimeNotificationHub>();
        this.AddSingleton<IRealtimeNotificationHub>(sp => sp.GetRequiredService<RealtimeNotificationHub>());
        // Behaviors run in array order, outermost first. Transient matches the old MediatR lifetimes (handlers take DbContext).
        this.AddMediator((MediatorOptions options) =>
        {
            // Outside the layer namespaces: the generated Mediator and its Internals namespace reference each other, which the namespace-cycle architecture rule would flag.
            options.Namespace = "ClearMeasure.Bootcamp.Generated.UiServerMediator";
            options.GenerateTypesAsInternal = true;
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>), typeof(FeatureFlagBehavior<,>)];
        });
        this.AddSingleton<IFeatureFlagEvaluator, RuntimeFeatureFlagEvaluator>();
        this.AddTransient<IBus>(provider => new ServerRealtimeBus(
            provider.GetRequiredService<IMediator>(),
            provider.GetRequiredService<IRealtimeNotificationHub>()));
    }

    private void ScanForHandlersAndHealthChecks() =>
        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<CanConnectToDatabaseHealthCheck>();
            scanner.AssemblyContainingType<HealthCheck>();
            scanner.AssemblyContainingType<Is64BitProcessHealthCheck>();
            scanner.AssemblyContainingType<CanConnectToLlmServerHealthCheck>();
            scanner.AssemblyContainingType<FunJeffreyCustomEventHealthCheck>();
            scanner.AssemblyContainingType<ToolProvider>();
        });

    private void AddHealthReporting()
    {
        this.AddHealthChecks()
            .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
            .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
            .AddCheck<Is64BitProcessHealthCheck>("Server")
            .AddCheck<HealthCheck>("API")
            .AddCheck<FunJeffreyCustomEventHealthCheck>("Jeffrey")
            .AddCheck<NeedsRebootHealthCheck>("NeedsReboot")
            .AddCheck<ProcessThreadCountHealthCheck>("ProcessThreadCount");
        this.AddSingleton<ILlmHealthCheckCache>(_ => new LlmHealthCheckCache());
        this.AddSingleton<IDetailedHealthReportProvider, DetailedHealthReportProvider>();
        this.AddSingleton<IHttpRequestMetricsCounter, HttpRequestMetricsCounter>();
    }
}
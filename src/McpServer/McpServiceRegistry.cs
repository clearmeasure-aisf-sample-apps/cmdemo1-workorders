using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.McpServer;

public class McpServiceRegistry : ServiceRegistry
{
    public McpServiceRegistry()
    {
        // Keep array types out of the service graph: the MCP SDK classifies a tool
        // parameter as DI-injectable when the provider reports IsService(parameterType),
        // which would omit array parameters (e.g. create-team's string[] memberUserNames)
        // from the tool input schema.
        this.Policies.Add(new ArrayTypesAreNotServicesPolicy());

        this.AddScoped<DbContext, DataContext>();
        AddBus();
        AddApplicationServices();
        ScanForValidators();

        // Register after scanning to override the auto-discovered DistributedBus
        // which requires NServiceBus IMessageSession (not available in the MCP server)
        this.AddTransient<IDistributedBus, NullDistributedBus>();
    }

    private void AddBus()
    {
        // AddMediatR owns all IRequestHandler<,> and INotificationHandler<> registrations;
        // the Lamar scan below must not connect those interfaces again.
        this.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<McpServiceRegistry>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(FeatureFlagBehavior<,>));
        });
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DataContext>());
        this.AddTransient<IPipelineBehavior<WorkOrderSpecificationQuery, WorkOrder[]>, PaginationCapGuard>();
        this.AddTransient<IBus>(provider => new Bus(provider.GetRequiredService<IMediator>()));
    }

    private void AddApplicationServices()
    {
        this.AddSingleton<IFeatureFlagEvaluator, RuntimeFeatureFlagEvaluator>();
        this.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
        this.AddSingleton(TimeProvider.System);
        this.AddSingleton<ChurchCalendar>();
        // Non-IValidator DI handles (#1824) — build deferred validators without registering them as IValidator<T>.
        this.AddSingleton(sp => new CreateVendorCommandShapeRules(
            (CreateVendorCommandValidator)ActivatorUtilities.CreateInstance(sp, typeof(CreateVendorCommandValidator))));
        this.AddSingleton(sp => new CreateTeamCommandShapeRules(
            (CreateTeamCommandValidator)ActivatorUtilities.CreateInstance(sp, typeof(CreateTeamCommandValidator))));
        this.AddSingleton(sp => new CreateDatedWorkOrdersCommandShapeRules(
            (CreateDatedWorkOrdersCommandValidator)ActivatorUtilities.CreateInstance(
                sp, typeof(CreateDatedWorkOrdersCommandValidator))));
    }

    // Scans Core exactly once (IBus and WebServiceMessageValidator are both in Core) and
    // registers only IValidator<> implementations; request/notification handlers come from
    // AddMediatR. Deferred create-command validators stay excluded so their shape-rule
    // factories remain the only registration.
    private void ScanForValidators() =>
        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<IBus>();
            scanner.AssemblyContainingType<DataContext>();
            scanner.Exclude(HttpEnvelopeValidationDeferral.IsDeferredValidator);
            scanner.ConnectImplementationsToTypesClosing(typeof(IValidator<>));
        });

    /// <summary>
    /// Reports an empty family for array types so Lamar never treats them (e.g. the
    /// string[] memberUserNames MCP tool parameter) as injectable services.
    /// </summary>
    private sealed class ArrayTypesAreNotServicesPolicy : IFamilyPolicy
    {
        public ServiceFamily? Build(Type type, ServiceGraph serviceGraph) =>
            type.IsArray ? new ServiceFamily(type, serviceGraph.DecoratorPolicies) : null;
    }
}

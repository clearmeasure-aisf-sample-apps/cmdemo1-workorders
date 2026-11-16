using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Every service the standalone MCP server resolves, named explicitly on
/// <see cref="Microsoft.Extensions.DependencyInjection"/> (no convention scanning), so the wiring reads top to bottom
/// and works under Native AOT.
/// </summary>
public static class McpServiceRegistry
{
    /// <summary>Registers the MCP server's application services, bus, and request validators.</summary>
    public static IServiceCollection AddMcpServerServices(this IServiceCollection services)
    {
        AddDataAccess(services);
        AddBus(services);
        AddApplicationServices(services);
        AddValidators(services);
        return services;
    }

    private static void AddDataAccess(IServiceCollection services)
    {
        services.AddScoped<DbContext, DataContext>();
        // Handlers that take the concrete DataContext get their own instance, as Lamar's concrete-type resolution did.
        services.AddTransient<DataContext>();
        services.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
    }

    private static void AddBus(IServiceCollection services)
    {
        // Named with generic arguments so the trimmer keeps Mediator's constructor; AddMediatR registers it by runtime Type.
        services.AddTransient<IMediator, Mediator>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DataContext>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(FeatureFlagBehavior<,>));
        });
        services.AddTransient<IBus>(provider => new Bus(provider.GetRequiredService<IMediator>()));
        // The MCP server has no NServiceBus endpoint, so domain events published to the distributed bus are dropped.
        services.AddTransient<IDistributedBus, NullDistributedBus>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlagEvaluator, RuntimeFeatureFlagEvaluator>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IWorkOrderNumberGenerator, WorkOrderNumberGenerator>();
        services.AddTransient<IWorkOrderBuilder, WorkOrderBuilder>();
    }

    /// <summary>
    /// The validators <see cref="ValidationBehavior{TRequest,TResponse}"/> runs for in-process requests. The create
    /// commands in <see cref="HttpEnvelopeValidationDeferral"/> are validated on the HTTP envelope only (#1824).
    /// </summary>
    private static void AddValidators(IServiceCollection services)
    {
        services.AddTransient<IValidator<DeleteSearchViewCommand>, DeleteSearchViewCommandValidator>();
        services.AddTransient<IValidator<EmployeeByUserNameQuery>, EmployeeByUserNameQueryValidator>();
        services.AddTransient<IValidator<EmployeeGetAllQuery>, EmployeeGetAllQueryValidator>();
        services.AddTransient<IValidator<HealthCheckRemotableRequest>, HealthCheckRemotableRequestValidator>();
        services.AddTransient<IValidator<InProgressToAssignedCommand>, InProgressToAssignedCommandValidator>();
        services.AddTransient<IValidator<RecentWorkOrdersQuery>, RecentWorkOrdersQueryValidator>();
        services.AddTransient<IValidator<RemoveAttachmentCommand>, RemoveAttachmentCommandValidator>();
        services.AddTransient<IValidator<SaveDraftCommand>, SaveDraftCommandValidator>();
        services.AddTransient<IValidator<SavedSearchViewsQuery>, SavedSearchViewsQueryValidator>();
        services.AddTransient<IValidator<SaveSearchViewCommand>, SaveSearchViewCommandValidator>();
        services.AddTransient<IValidator<UserLoggedInEvent>, UserLoggedInEventValidator>();
        services.AddTransient<IValidator<WebServiceMessage>, WebServiceMessageValidator>();
        services.AddTransient<IValidator<WorkOrderAttachmentsQuery>, WorkOrderAttachmentsQueryValidator>();
        services.AddTransient<IValidator<WorkOrderByNumberQuery>, WorkOrderByNumberQueryValidator>();
        services.AddTransient<IValidator<WorkOrdersByNumbersQuery>, WorkOrdersByNumbersQueryValidator>();
        services.AddTransient<IValidator<WorkOrderSearchSummaryQuery>, WorkOrderSearchSummaryQueryValidator>();
        services.AddTransient<IValidator<WorkOrderSpecificationQuery>, WorkOrderSpecificationQueryValidator>();
        services.AddTransient<IValidator<WorkOrderStatusHistoryQuery>, WorkOrderStatusHistoryQueryValidator>();
    }
}

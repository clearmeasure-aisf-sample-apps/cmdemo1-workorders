using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.McpServer;

public class McpServiceRegistry : ServiceRegistry
{
    public McpServiceRegistry()
    {
        this.AddScoped<DbContext, DataContext>();
        AddBus();
        AddApplicationServices();
        ScanForHandlers();

        // Register after scanning to override the auto-discovered DistributedBus
        // which requires NServiceBus IMessageSession (not available in the MCP server)
        this.AddTransient<IDistributedBus, NullDistributedBus>();
    }

    private void AddBus()
    {
        this.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<McpServiceRegistry>();
            cfg.AddOpenBehavior(typeof(FeatureFlagBehavior<,>));
        });
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DataContext>());
        this.AddTransient<IBus>(provider => new Bus(provider.GetRequiredService<IMediator>()));
    }

    private void AddApplicationServices()
    {
        this.AddSingleton<IFeatureFlagEvaluator, RuntimeFeatureFlagEvaluator>();
        this.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
        this.AddSingleton(TimeProvider.System);
    }

    private void ScanForHandlers() =>
        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<IBus>();
            scanner.AssemblyContainingType<DataContext>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });
}

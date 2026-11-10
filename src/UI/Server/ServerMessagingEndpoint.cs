using ClearMeasure.Bootcamp.DataAccess.Messaging;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The NServiceBus endpoint UI.Server hosts: the SQL Server transport against the application database, or the
/// learning transport when the connection string is a file database (<c>Data Source=</c>: LocalDB and SQLite hosts).
/// </summary>
public static class ServerMessagingEndpoint
{
    private const string EndpointName = "UI.Server";

    public static bool ShouldUseLearningTransport(string? sqlConnectionString) =>
        !string.IsNullOrEmpty(sqlConnectionString)
        && sqlConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

    public static void Register(WebApplicationBuilder builder)
    {
        var endpointConfiguration = new EndpointConfiguration(EndpointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.EnableOpenTelemetry();
        UseTransport(endpointConfiguration, builder.Configuration.GetConnectionString("SqlConnectionString") ?? "");
        endpointConfiguration.Conventions().Add(new MessagingConventions());
        // Worker hosts WorkOrderProcessing; scanning Worker.dll here discovers sagas without
        // matching persistence when LearningTransport is used (unit/integration hosts).
        endpointConfiguration.AssemblyScanner().ExcludeAssemblies("Worker.dll");
        builder.Host.UseNServiceBus(_ => endpointConfiguration);
    }

    private static void UseTransport(EndpointConfiguration endpointConfiguration, string sqlConnectionString)
    {
        if (ShouldUseLearningTransport(sqlConnectionString))
        {
            endpointConfiguration.UseTransport<LearningTransport>();
            return;
        }

        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(sqlConnectionString);
        transport.DefaultSchema("nServiceBus");
        transport.Transactions(TransportTransactionMode.TransactionScope);
    }
}

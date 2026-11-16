using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Operator endpoints outside the versioned contract: <c>POST _diagnostics/reset-db-connections</c> and
/// <c>GET _diagnostics/services</c> (the container's registrations).
/// </summary>
public static class DiagnosticEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("_diagnostics/reset-db-connections", ResetDbConnections);
        app.MapGet("_diagnostics/services", Services);
    }

    public static IResult ResetDbConnections(IDatabaseConfiguration databaseConfiguration)
    {
        databaseConfiguration.ResetConnectionPool();
        return TypedResults.Text("Connection pools cleared", "text/plain; charset=utf-8");
    }

    public static string Services(RegisteredServices registeredServices) => registeredServices.Describe();
}

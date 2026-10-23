using System.Globalization;
using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Writes the <c>/_healthcheck</c> body: the status word, then <c>databaseVersion</c>.
/// </summary>
public static class HealthCheckPlainTextResponseWriter
{
    /// <summary>
    /// Writes the plain-text health answer for <paramref name="report"/>.
    /// </summary>
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        var version = await TryReadVersionAsync(context);
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(Format(report.Status, version), context.RequestAborted);
    }

    /// <summary>
    /// Builds the two-line body. The first line is the status word; the second is <c>databaseVersion</c>.
    /// </summary>
    internal static string Format(HealthStatus status, int? version)
    {
        var versionText = version is null
            ? "null"
            : version.Value.ToString(CultureInfo.InvariantCulture);
        return $"{status}\ndatabaseVersion: {versionText}";
    }

    private static async Task<int?> TryReadVersionAsync(HttpContext context)
    {
        var services = (IServiceProvider?)context.RequestServices;
        if (services is null)
        {
            return null;
        }

        try
        {
            await using var scope = services.CreateAsyncScope();
            var reader = scope.ServiceProvider.GetService<DatabaseMigrationVersionReader>();
            if (reader is null)
            {
                return null;
            }

            return await reader.ReadAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger(nameof(HealthCheckPlainTextResponseWriter));
            logger?.LogWarning(ex, "Could not read databaseVersion for the health check");
            return null;
        }
    }
}

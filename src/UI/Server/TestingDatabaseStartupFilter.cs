using ClearMeasure.Bootcamp.DataAccess.Mappings;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Ensures SQLite schema exists (and idempotently adds tables missing from an existing file) when hosting UI.Server on SQLite.
/// </summary>
internal sealed class TestingDatabaseStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    DataAccess.SqliteSchemaUpgrade.EnsureCreatedAndUpgraded(db);
                }
            }

            next(app);
        };
    }
}

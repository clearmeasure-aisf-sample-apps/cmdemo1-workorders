using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[SetUpFixture]
public class SqliteDatabaseSetup
{
    [OneTimeSetUp]
    public async Task SetUp()
    {
        var context = TestHost.GetRequiredService<DbContext>();
        if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            ClearMeasure.Bootcamp.DataAccess.SqliteSchemaUpgrade.EnsureCreatedAndUpgraded(context);
            // The SQLite database file persists across runs, so reset it before each run.
            // Tests are written against a fresh database (CI runs from a clean checkout);
            // stale rows otherwise push new employees past the first page of
            // EmployeeGetAllQuery and break paging assumptions.
            await TruncateAllTablesAsync(context);
        }
    }

    private static async Task TruncateAllTablesAsync(DbContext context)
    {
        var pending = context.Model.GetEntityTypes().ToList();
        while (pending.Count > 0)
        {
            foreach (var type in NextEmptyableTables(pending))
            {
                await EmptyTableAsync(context, type);
                pending.Remove(type);
            }
        }
    }

    /// <summary>A table can be emptied only after every table that references it is.</summary>
    private static IReadOnlyList<IEntityType> NextEmptyableTables(IReadOnlyList<IEntityType> pending)
    {
        var safe = pending
            .Where(type => pending
                .Where(other => other != type)
                .All(other => other.GetDeclaredForeignKeys().All(fk => fk.PrincipalEntityType != type)))
            .ToList();
        // If the remaining tables reference each other (a cycle), start from the first one
        // anyway so the loop can make progress.
        return safe.Count > 0 ? safe : [pending.First()];
    }

    private static async Task EmptyTableAsync(DbContext context, IEntityType type)
    {
        // SQLite does not support the "dbo" schema in raw SQL; EF strips it
        // from queries, so raw statements must use the table name alone.
        var table = type.GetTableName();
        var qualified = $"\"{table}\"";
        // Table and schema names come from the EF model, not user input.
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM {qualified};");
#pragma warning restore EF1002
    }
}

using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore.Design;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Used only by EF's build-time code generation (compiled model and precompiled queries for Native AOT).
/// The generated model is provider-specific, so it is generated for SQL Server, the production database.
/// Nothing connects to this connection string.
/// </summary>
public class DesignTimeDataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args) => new(new DesignTimeDatabaseConfiguration());

    private sealed class DesignTimeDatabaseConfiguration : IDatabaseConfiguration
    {
        public string GetConnectionString() => "Server=design-time;Database=ChurchBulletin;Integrated Security=true;TrustServerCertificate=true";

        public void ResetConnectionPool()
        {
        }
    }
}
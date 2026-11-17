using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

/// <summary>EF's design-time tools (compiled model, precompiled queries) get a SQL Server context without a database.</summary>
[TestFixture]
public class DesignTimeDataContextFactoryTests
{
    [Test]
    public void CreateDbContext_Should_UseSqlServer_WithoutOpeningAConnection()
    {
        using var context = new DesignTimeDataContextFactory().CreateDbContext([]);

        context.Database.IsSqlServer().ShouldBeTrue();
        context.Database.GetDbConnection().State.ShouldBe(System.Data.ConnectionState.Closed);
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotRecentWorkOrdersQueryHandler"/> lists the newest activity in the same order as the JIT handler.</summary>
[TestFixture]
public class AotRecentWorkOrdersQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_ListTheSameWorkOrdersInTheSameOrder()
    {
        var jit = await new RecentWorkOrdersQueryHandler(_db.Context).Handle(new RecentWorkOrdersQuery(5));
        var aot = await new AotRecentWorkOrdersQueryHandler(_db.Context, _db.Clock).Handle(new RecentWorkOrdersQuery(5));

        aot.Select(order => order.Number).ShouldBe(jit.Select(order => order.Number));
        aot.ShouldNotBeEmpty();
    }
}

using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotWorkOrdersByNumbersQueryHandler"/> finds the same work orders by number as the JIT handler (neither promises an order).</summary>
[TestFixture]
public class AotWorkOrdersByNumbersQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_FindTheSameWorkOrders()
    {
        var jit = await new WorkOrdersByNumbersQueryHandler(_db.Context).Handle(new WorkOrdersByNumbersQuery(["AAA0003", "AAA0001", "ZZZ9999"]));
        var aot = await new AotWorkOrdersByNumbersQueryHandler(_db.Context, _db.Clock).Handle(new WorkOrdersByNumbersQuery(["AAA0003", "AAA0001", "ZZZ9999"]));

        aot.Select(order => order.Number).ShouldBe(jit.Select(order => order.Number), ignoreOrder: true);
        aot.Length.ShouldBe(2);
    }
}

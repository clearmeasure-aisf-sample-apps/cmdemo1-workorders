using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotWorkOrderReportsQueryHandler"/> answers the reports page exactly as the JIT handler does.</summary>
[TestFixture]
public class AotWorkOrderReportsQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler()
    {
        var jit = await new WorkOrderReportsQueryHandler(_db.Context, _db.Clock).Handle(new WorkOrderReportsQuery());
        var aot = await new AotWorkOrderReportsQueryHandler(_db.Context, _db.Clock).Handle(new WorkOrderReportsQuery());

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

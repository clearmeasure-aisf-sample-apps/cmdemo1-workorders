using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotWorkOrderSearchSummaryQueryHandler"/> answers the search summary exactly as the JIT handler does.</summary>
[TestFixture]
public class AotWorkOrderSearchSummaryQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_ForTheAssignee()
    {
        var jit = await new WorkOrderSearchSummaryQueryHandler(_db.Context, _db.Clock).Handle(new WorkOrderSearchSummaryQuery("ffixer"));
        var aot = await new AotWorkOrderSearchSummaryQueryHandler(_db.Context, _db.Clock).Handle(new WorkOrderSearchSummaryQuery("ffixer"));

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

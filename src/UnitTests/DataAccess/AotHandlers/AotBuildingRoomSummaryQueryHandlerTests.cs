using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotBuildingRoomSummaryQueryHandler"/> answers the building and room counts exactly as the JIT handler does.</summary>
[TestFixture]
public class AotBuildingRoomSummaryQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler()
    {
        var jit = await new BuildingRoomSummaryQueryHandler(_db.Context, _db.Clock).Handle(new BuildingRoomSummaryQuery());
        var aot = await new AotBuildingRoomSummaryQueryHandler(_db.Context, _db.Clock).Handle(new BuildingRoomSummaryQuery());

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

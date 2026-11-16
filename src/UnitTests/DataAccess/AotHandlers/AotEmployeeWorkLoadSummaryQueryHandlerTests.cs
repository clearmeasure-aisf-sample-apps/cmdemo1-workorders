using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotEmployeeWorkLoadSummaryQueryHandler"/> answers the load of every assignable employee exactly as the JIT handler does.</summary>
[TestFixture]
public class AotEmployeeWorkLoadSummaryQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler()
    {
        var jit = await new EmployeeWorkLoadSummaryQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadSummaryQuery());
        var aot = await new AotEmployeeWorkLoadSummaryQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadSummaryQuery());

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

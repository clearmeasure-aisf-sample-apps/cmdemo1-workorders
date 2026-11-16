using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotEmployeeWorkLoadQueryHandler"/> answers the load of one employee exactly as the JIT handler does.</summary>
[TestFixture]
public class AotEmployeeWorkLoadQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_ForAnEmployeeWithOpenAndOverdueWork()
    {
        var jit = await new EmployeeWorkLoadQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadQuery("ffixer"));
        var aot = await new AotEmployeeWorkLoadQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadQuery("ffixer"));

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_ForAnEmployeeWithNoWork()
    {
        var jit = await new EmployeeWorkLoadQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadQuery("iidle"));
        var aot = await new AotEmployeeWorkLoadQueryHandler(_db.Context, _db.Clock).Handle(new EmployeeWorkLoadQuery("iidle"));

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

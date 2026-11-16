using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary><see cref="AotNotificationsPanelQueryHandler"/> answers the notifications panel exactly as the JIT handler does.</summary>
[TestFixture]
public class AotNotificationsPanelQueryHandlerTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_ForTheAssignee()
    {
        var jit = await new NotificationsPanelQueryHandler(_db.Context, _db.Clock).Handle(new NotificationsPanelQuery("ffixer"));
        var aot = await new AotNotificationsPanelQueryHandler(_db.Context, _db.Clock).Handle(new NotificationsPanelQuery("ffixer"));

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_ForAnUnknownUser()
    {
        var jit = await new NotificationsPanelQueryHandler(_db.Context, _db.Clock).Handle(new NotificationsPanelQuery("nobody"));
        var aot = await new AotNotificationsPanelQueryHandler(_db.Context, _db.Clock).Handle(new NotificationsPanelQuery("nobody"));

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
    }
}

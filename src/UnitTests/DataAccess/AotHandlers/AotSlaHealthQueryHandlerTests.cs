using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// <see cref="AotSlaHealthQueryHandler"/> answers the same SLA health as <see cref="SlaHealthQueryHandler"/> on one seeded
/// database, through the remotable contract the browser receives.
/// </summary>
[TestFixture]
public class AotSlaHealthQueryHandlerTests
{
    [Test]
    public async Task Handle_Should_MatchTheJitHandler_OnTheSharedSeed()
    {
        await using var database = await AotParityDatabase.SeedAsync();

        var health = await BothHandlersAnswerAlike(database);

        health.CurrentMonthOrderCount.ShouldBe(0);
        health.LastMonthOrderCount.ShouldBe(0);
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_When_NothingIsCompleted()
    {
        await using var database = await AotParityDatabase.SeedAsync(SlaHealthCasesMother.NoneCompleted);

        var health = await BothHandlersAnswerAlike(database);

        health.ShouldBeEquivalentTo(Expected(current: (null, 0), last: (null, 0)));
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_When_CompletionsHaveAndLackDueDates()
    {
        await using var database = await AotParityDatabase.SeedAsync(SlaHealthCasesMother.CompletedWithAndWithoutDueDate);

        var health = await BothHandlersAnswerAlike(database);

        health.ShouldBeEquivalentTo(Expected(current: (100m, 1), last: (100m, 1)));
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_When_WorkOrdersAreOverdue()
    {
        await using var database = await AotParityDatabase.SeedAsync(SlaHealthCasesMother.Overdue);

        var health = await BothHandlersAnswerAlike(database);

        health.ShouldBeEquivalentTo(Expected(current: (50m, 2), last: (0m, 1)));
    }

    [Test]
    public async Task Handle_Should_MatchTheJitHandler_When_TheChurchDateDiffersFromTheUtcDate()
    {
        await using var database = await AotParityDatabase.SeedAsync(SlaHealthCasesMother.AcrossChurchDateBoundaries);

        var health = await BothHandlersAnswerAlike(database);

        health.ShouldBeEquivalentTo(Expected(current: (100m, 1), last: (100m, 1)));
    }

    private static async Task<SlaHealth> BothHandlersAnswerAlike(AotParityDatabase database)
    {
        var jit = await new SlaHealthQueryHandler(database.Context, database.Clock).Handle(new SlaHealthQuery());
        var aot = await new AotSlaHealthQueryHandler(database.Context, database.Clock).Handle(new SlaHealthQuery());

        AotParityDatabase.Json(aot).ShouldBe(AotParityDatabase.Json(jit));
        return aot;
    }

    private static SlaHealth Expected((decimal? Percentage, int Count) current, (decimal? Percentage, int Count) last) => new()
    {
        CurrentMonthPercentage = current.Percentage,
        CurrentMonthOrderCount = current.Count,
        LastMonthPercentage = last.Percentage,
        LastMonthOrderCount = last.Count
    };
}

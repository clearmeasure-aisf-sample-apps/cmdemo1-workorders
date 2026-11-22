using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class SlaCompletionTests
{
    [Test]
    public void Should_CompletedBeSameDay_When_StoredInstantIsNoonUtc()
    {
        // 2026-10-15 12:00:00 UTC = 2026-10-15 07:00:00 CDT
        var sla = new SlaCompletion(new DateOnly(2026, 10, 15), new DateTime(2026, 10, 15, 12, 0, 0));

        sla.Completed.ShouldBe(new DateOnly(2026, 10, 15));
    }

    [Test]
    public void Should_CompletedBePreviousDay_When_StoredEveningLocal_NextDayUtc()
    {
        // 2026-10-16 01:00:00 UTC = 2026-10-15 19:00:00 CDT, so the church date is the due date
        var sla = new SlaCompletion(new DateOnly(2026, 10, 15), new DateTime(2026, 10, 16, 1, 0, 0));

        sla.Completed.ShouldBe(new DateOnly(2026, 10, 15));
    }

    [Test]
    public void Should_IsOnTime_When_CompletedOnDueDate()
    {
        var sla = new SlaCompletion(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 10, 23, 59, 0));

        sla.IsOnTime.ShouldBeTrue();
    }

    [Test]
    public void Should_IsNotOnTime_When_CompletedAfterDueDate()
    {
        var sla = new SlaCompletion(new DateOnly(2026, 5, 10), new DateTime(2026, 5, 11, 5, 1, 0));

        sla.IsOnTime.ShouldBeFalse();
    }
}

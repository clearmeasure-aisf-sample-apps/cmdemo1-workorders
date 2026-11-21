using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class SlaHealthCompletionWindowTests
{
    [Test]
    public void Should_ReturnUnspecifiedChurchMidnightBounds_When_NormalMonth()
    {
        var (start, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        start.ShouldBe(new DateTime(2026, 7, 1, 5, 0, 0));
        end.ShouldBe(new DateTime(2026, 8, 1, 5, 0, 0));
        start.Kind.ShouldBe(DateTimeKind.Unspecified);
        end.Kind.ShouldBe(DateTimeKind.Unspecified);
        (end - start).ShouldBe(TimeSpan.FromDays(31));
    }

    [Test]
    public void Should_ShortenByOneHour_When_MarchSpansSpringForward()
    {
        var (start, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        start.ShouldBe(new DateTime(2026, 3, 1, 6, 0, 0));
        end.ShouldBe(new DateTime(2026, 4, 1, 5, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromDays(31) - TimeSpan.FromHours(1));
    }

    [Test]
    public void Should_IncludeLastLocalSecondAndExcludeNextSecond_When_WindowEnd()
    {
        var (start, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        // Oct 31 23:59:59 CDT is the last local second of the window; Nov 1 05:00:00 UTC is the exclusive end.
        var lastLocalSecond = new DateTime(2026, 11, 1, 4, 59, 59);
        var oneSecondLater = new DateTime(2026, 11, 1, 5, 0, 0);

        (lastLocalSecond >= start && lastLocalSecond < end).ShouldBeTrue();
        (oneSecondLater >= start && oneSecondLater < end).ShouldBeFalse();
    }

    [Test]
    public void Should_LengthenByOneHour_When_NovemberSpansFallBack()
    {
        var (start, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        start.ShouldBe(new DateTime(2026, 11, 1, 5, 0, 0));
        end.ShouldBe(new DateTime(2026, 12, 1, 6, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromDays(30) + TimeSpan.FromHours(1));
    }

    [Test]
    public void Should_Return24HourWindow_When_MonthStartEqualsMonthEnd()
    {
        var (start, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 2, 14), new DateOnly(2026, 2, 14));

        start.ShouldBe(new DateTime(2026, 2, 14, 6, 0, 0));
        end.ShouldBe(new DateTime(2026, 2, 15, 6, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromHours(24));
    }

    [Test]
    public void Should_MapEndMinusOneTick_ToMonthEnd_When_SingleDayWindow()
    {
        var (_, end) = SlaHealth.CompletionWindow(new DateOnly(2026, 2, 14), new DateOnly(2026, 2, 14));

        SlaHealth.ChurchDate(end.AddTicks(-1)).ShouldBe(new DateOnly(2026, 2, 14));
    }

    [Test]
    public void Should_ReturnPreviousChurchDate_When_0530ZOnCstDate()
    {
        // 05:30 UTC is 23:30 CST (UTC-06:00) the previous church day; the input is Unspecified,
        // the shape of persisted CompletedDate values.
        SlaHealth.ChurchDate(new DateTime(2026, 1, 15, 5, 30, 0)).ShouldBe(new DateOnly(2026, 1, 14));
    }

    [Test]
    public void Should_ReturnSameChurchDate_When_0600ZOnCstDate()
    {
        // 06:00 UTC is exactly local midnight (CST, UTC-06:00) of the same church date; the input is
        // Unspecified, the shape of persisted CompletedDate values.
        SlaHealth.ChurchDate(new DateTime(2026, 1, 15, 6, 0, 0)).ShouldBe(new DateOnly(2026, 1, 15));
    }
}

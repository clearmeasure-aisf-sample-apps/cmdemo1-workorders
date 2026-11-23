using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneCompletionWindowTests
{
    [Test]
    public void Should_ReturnUnspecifiedChurchMidnightBounds_When_NormalMonth()
    {
        var july = new ChurchMonth(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var window = ChurchTimeZone.CompletionWindow(july, july);

        window.Start.ShouldBe(new DateTime(2026, 7, 1, 5, 0, 0));
        window.EndExcluded.ShouldBe(new DateTime(2026, 8, 1, 5, 0, 0));
        window.Start.Kind.ShouldBe(DateTimeKind.Unspecified);
        window.EndExcluded.Kind.ShouldBe(DateTimeKind.Unspecified);
        (window.EndExcluded - window.Start).ShouldBe(TimeSpan.FromDays(31));
    }

    [Test]
    public void Should_ShortenByOneHour_When_MarchSpansSpringForward()
    {
        var march = new ChurchMonth(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        var window = ChurchTimeZone.CompletionWindow(march, march);

        window.Start.ShouldBe(new DateTime(2026, 3, 1, 6, 0, 0));
        window.EndExcluded.ShouldBe(new DateTime(2026, 4, 1, 5, 0, 0));
        (window.EndExcluded - window.Start).ShouldBe(TimeSpan.FromDays(31) - TimeSpan.FromHours(1));
    }

    [Test]
    public void Should_IncludeLastLocalSecondAndExcludeNextSecond_When_WindowEnd()
    {
        var october = new ChurchMonth(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        var window = ChurchTimeZone.CompletionWindow(october, october);

        // Oct 31 23:59:59 CDT is the last local second of the window; Nov 1 05:00:00 UTC is the exclusive end.
        var lastLocalSecond = new DateTime(2026, 11, 1, 4, 59, 59);
        var oneSecondLater = new DateTime(2026, 11, 1, 5, 0, 0);

        window.Contains(lastLocalSecond).ShouldBeTrue();
        window.Contains(oneSecondLater).ShouldBeFalse();
    }

    [Test]
    public void Should_LengthenByOneHour_When_NovemberSpansFallBack()
    {
        var november = new ChurchMonth(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        var window = ChurchTimeZone.CompletionWindow(november, november);

        window.Start.ShouldBe(new DateTime(2026, 11, 1, 5, 0, 0));
        window.EndExcluded.ShouldBe(new DateTime(2026, 12, 1, 6, 0, 0));
        (window.EndExcluded - window.Start).ShouldBe(TimeSpan.FromDays(30) + TimeSpan.FromHours(1));
    }

    [Test]
    public void Should_Return24HourWindow_When_MonthStartEqualsMonthEnd()
    {
        var oneDay = new ChurchMonth(new DateOnly(2026, 2, 14), new DateOnly(2026, 2, 14));
        var window = ChurchTimeZone.CompletionWindow(oneDay, oneDay);

        window.Start.ShouldBe(new DateTime(2026, 2, 14, 6, 0, 0));
        window.EndExcluded.ShouldBe(new DateTime(2026, 2, 15, 6, 0, 0));
        (window.EndExcluded - window.Start).ShouldBe(TimeSpan.FromHours(24));
    }

    [Test]
    public void Should_MapEndMinusOneTick_ToMonthEnd_When_SingleDayWindow()
    {
        var oneDay = new ChurchMonth(new DateOnly(2026, 2, 14), new DateOnly(2026, 2, 14));
        var window = ChurchTimeZone.CompletionWindow(oneDay, oneDay);

        ChurchTimeZone.ChurchDate(window.EndExcluded.AddTicks(-1)).ShouldBe(new DateOnly(2026, 2, 14));
    }

    [Test]
    public void Should_ReturnPreviousChurchDate_When_0530ZOnCstDate()
    {
        // 05:30 UTC is 23:30 CST (UTC-06:00) the previous church day; the input is Unspecified,
        // the shape of persisted CompletedDate values.
        ChurchTimeZone.ChurchDate(new DateTime(2026, 1, 15, 5, 30, 0)).ShouldBe(new DateOnly(2026, 1, 14));
    }

    [Test]
    public void Should_ReturnSameChurchDate_When_0600ZOnCstDate()
    {
        // 06:00 UTC is exactly local midnight (CST, UTC-06:00) of the same church date; the input is
        // Unspecified, the shape of persisted CompletedDate values.
        ChurchTimeZone.ChurchDate(new DateTime(2026, 1, 15, 6, 0, 0)).ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Test]
    public void Should_BoundCompletionWindow_ToTheChurchMonth()
    {
        var monthStart = new DateOnly(2026, 10, 1);
        var monthEnd = new DateOnly(2026, 10, 31);
        var october = new ChurchMonth(monthStart, monthEnd);
        var window = ChurchTimeZone.CompletionWindow(october, october);

        ChurchTimeZone.ChurchDate(window.Start).ShouldBe(monthStart);
        ChurchTimeZone.ChurchDate(window.Start.AddTicks(-1)).ShouldBe(monthStart.AddDays(-1));
        ChurchTimeZone.ChurchDate(window.EndExcluded.AddTicks(-1)).ShouldBe(monthEnd);
        ChurchTimeZone.ChurchDate(window.EndExcluded).ShouldBe(monthEnd.AddDays(1));
        window.Start.Kind.ShouldBe(DateTimeKind.Unspecified);
        window.EndExcluded.Kind.ShouldBe(DateTimeKind.Unspecified);

        var lateEveningUtc = new DateTime(2026, 11, 1, 3, 0, 0);
        window.Contains(lateEveningUtc).ShouldBeTrue();
    }

    [Test]
    public void Should_ReturnChurchDate_When_CompletedLateEveningLocal()
    {
        var result = ChurchTimeZone.ChurchDate(new DateTime(2026, 11, 1, 3, 0, 0));

        result.ShouldBe(new DateOnly(2026, 10, 31));
    }
}

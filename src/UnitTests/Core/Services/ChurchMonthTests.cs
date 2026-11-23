using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchMonthTests
{
    [TestCase("2026-11-09", "2026-11-01", "2026-11-30")]
    [TestCase("2028-02-29", "2028-02-01", "2028-02-29")]
    public void ShouldSpanTheCalendarMonth_WhenBuiltFromADay(string day, string firstDay, string lastDay)
    {
        var churchMonth = ChurchMonth.Containing(DateOnly.Parse(day, CultureInfo.InvariantCulture));

        churchMonth.ShouldBe(new ChurchMonth(
            DateOnly.Parse(firstDay, CultureInfo.InvariantCulture),
            DateOnly.Parse(lastDay, CultureInfo.InvariantCulture)));
    }

    [Test]
    public void ShouldStepBackAcrossTheYear_WhenThePreviousMonthIsAsked()
    {
        var january = ChurchMonth.Containing(new DateOnly(2027, 1, 15));

        var previous = january.Previous();

        previous.ShouldBe(new ChurchMonth(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)));
    }

    [TestCase(1)]
    [TestCase(30)]
    public void ShouldContainItsOwnDays_WhenADayOfTheMonthIsChecked(int day)
    {
        var november = ChurchMonth.Containing(new DateOnly(2026, 11, 9));

        var contains = november.Contains(new DateOnly(2026, 11, day));

        contains.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotContainTheNextMonthsFirstDay_WhenItIsChecked()
    {
        var november = ChurchMonth.Containing(new DateOnly(2026, 11, 9));

        var contains = november.Contains(new DateOnly(2026, 12, 1));

        contains.ShouldBeFalse();
    }

    [Test]
    public void ShouldSpanThirtyOneDays_WhenNormalMonth()
    {
        var july = ChurchMonth.Containing(new DateOnly(2026, 7, 15));

        var (start, end) = july.CompletionWindow(july);

        start.ShouldBe(new DateTime(2026, 7, 1, 5, 0, 0));
        end.ShouldBe(new DateTime(2026, 8, 1, 5, 0, 0));
        start.Kind.ShouldBe(DateTimeKind.Unspecified);
        end.Kind.ShouldBe(DateTimeKind.Unspecified);
        (end - start).ShouldBe(TimeSpan.FromDays(31));
    }

    [Test]
    public void ShouldShortenByOneHour_WhenMarchSpansSpringForward()
    {
        var march = ChurchMonth.Containing(new DateOnly(2026, 3, 15));

        var (start, end) = march.CompletionWindow(march);

        start.ShouldBe(new DateTime(2026, 3, 1, 6, 0, 0));
        end.ShouldBe(new DateTime(2026, 4, 1, 5, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromDays(31) - TimeSpan.FromHours(1));
    }

    [Test]
    public void ShouldLengthenByOneHour_WhenNovemberSpansFallBack()
    {
        var november = ChurchMonth.Containing(new DateOnly(2026, 11, 15));

        var (start, end) = november.CompletionWindow(november);

        start.ShouldBe(new DateTime(2026, 11, 1, 5, 0, 0));
        end.ShouldBe(new DateTime(2026, 12, 1, 6, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromDays(30) + TimeSpan.FromHours(1));
    }

    [Test]
    public void ShouldSpanMultipleMonths_WhenSinceMonthIsEarlier()
    {
        var october = ChurchMonth.Containing(new DateOnly(2026, 10, 15));
        var december = ChurchMonth.Containing(new DateOnly(2026, 12, 15));

        var (start, end) = december.CompletionWindow(october);

        start.ShouldBe(new DateTime(2026, 10, 1, 5, 0, 0));
        end.ShouldBe(new DateTime(2027, 1, 1, 6, 0, 0));
        (end - start).ShouldBe(TimeSpan.FromDays(92) + TimeSpan.FromHours(1));
    }

    [Test]
    public void ShouldMapEndMinusOneTickToLastDayOfMonth_WhenCompletionWindowIsBuilt()
    {
        var february = ChurchMonth.Containing(new DateOnly(2026, 2, 14));
        var (_, end) = february.CompletionWindow(february);
        ChurchTimeZone.ChurchDate(end.AddTicks(-1)).ShouldBe(new DateOnly(2026, 2, 28));
    }

    [Test]
    public void ShouldBoundCompletionWindowToChurchMonth_WhenCompletionWindowIsBuilt()
    {
        var october = ChurchMonth.Containing(new DateOnly(2026, 10, 15));
        var (start, end) = october.CompletionWindow(october);

        ChurchTimeZone.ChurchDate(start).ShouldBe(new DateOnly(2026, 10, 1));
        ChurchTimeZone.ChurchDate(start.AddTicks(-1)).ShouldBe(new DateOnly(2026, 9, 30));
        ChurchTimeZone.ChurchDate(end.AddTicks(-1)).ShouldBe(new DateOnly(2026, 10, 31));
        ChurchTimeZone.ChurchDate(end).ShouldBe(new DateOnly(2026, 11, 1));
        start.Kind.ShouldBe(DateTimeKind.Unspecified);
        end.Kind.ShouldBe(DateTimeKind.Unspecified);

        var lastLocalSecond = new DateTime(2026, 11, 1, 4, 59, 59);
        (lastLocalSecond >= start && lastLocalSecond < end).ShouldBeTrue();

        var oneSecondLater = new DateTime(2026, 11, 1, 5, 0, 0);
        (oneSecondLater >= start && oneSecondLater < end).ShouldBeFalse();

        var lateEveningUtc = new DateTime(2026, 11, 1, 3, 0, 0);
        (lateEveningUtc >= start && lateEveningUtc < end).ShouldBeTrue();
    }
}

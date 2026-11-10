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
}

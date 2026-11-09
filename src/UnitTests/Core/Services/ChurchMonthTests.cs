using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchMonthTests
{
    [TestCase(2026, 11, 9, 2026, 11, 1, 2026, 11, 30)]
    [TestCase(2028, 2, 29, 2028, 2, 1, 2028, 2, 29)]
    public void ShouldSpanTheCalendarMonth_WhenBuiltFromADay(
        int year, int month, int day, int firstYear, int firstMonth, int firstDay, int lastYear, int lastMonth, int lastDay)
    {
        var churchMonth = ChurchMonth.Containing(new DateOnly(year, month, day));

        churchMonth.ShouldBe(new ChurchMonth(new DateOnly(firstYear, firstMonth, firstDay), new DateOnly(lastYear, lastMonth, lastDay)));
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

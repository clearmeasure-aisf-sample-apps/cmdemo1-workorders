using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class CompletionWeeksTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);

    [TestCase("2026-11-08", "2026-11-08")]
    [TestCase("2026-11-14", "2026-11-08")]
    [TestCase("2026-11-09", "2026-11-08")]
    public void ShouldNameAWeekByItsMostRecentSunday_WhenADayIsGiven(string day, string expected)
    {
        var weekEnding = CompletionWeeks.WeekEnding(DateOnly.Parse(day, CultureInfo.InvariantCulture));

        weekEnding.ShouldBe(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Test]
    public void ShouldCountEveryWeekOldestFirst_WhenSomeWeeksHaveNoCompletions()
    {
        var weeks = new CompletionWeeks(Monday, 3);

        var counts = weeks.CountCompletions(
        [
            new DateTime(2026, 11, 9, 10, 0, 0),
            new DateTime(2026, 11, 8, 10, 0, 0),
            new DateTime(2026, 10, 26, 10, 0, 0)
        ]);

        counts.Select(week => (week.WeekEndingDate, week.Count)).ShouldBe(
        [
            (new DateOnly(2026, 10, 25), 1),
            (new DateOnly(2026, 11, 1), 0),
            (new DateOnly(2026, 11, 8), 2)
        ]);
    }

    [Test]
    public void ShouldIgnoreACompletion_WhenItFallsBeforeTheFirstWeek()
    {
        var weeks = new CompletionWeeks(Monday, 1);

        var counts = weeks.CountCompletions([new DateTime(2026, 10, 1, 10, 0, 0)]);

        counts.Single().Count.ShouldBe(0);
        weeks.StartDate.ShouldBe(new DateOnly(2026, 11, 2));
    }
}

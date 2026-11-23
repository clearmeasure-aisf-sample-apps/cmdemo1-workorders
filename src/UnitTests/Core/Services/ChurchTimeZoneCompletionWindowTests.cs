using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneCompletionWindowTests
{
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
    public void Should_ReturnChurchDate_When_CompletedLateEveningLocal()
    {
        var result = ChurchTimeZone.ChurchDate(new DateTime(2026, 11, 1, 3, 0, 0));

        result.ShouldBe(new DateOnly(2026, 10, 31));
    }
}

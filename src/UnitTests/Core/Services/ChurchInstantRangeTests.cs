using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchInstantRangeTests
{
    [Test]
    public void Should_ContainStartAndEveryTickBeforeEnd_When_BoundsAreChecked()
    {
        var start = new DateTime(2026, 10, 1, 5, 0, 0);
        var end = new DateTime(2026, 11, 1, 5, 0, 0);

        var range = new ChurchInstantRange(start, end);

        range.Contains(start).ShouldBeTrue();
        range.Contains(start.AddTicks(-1)).ShouldBeFalse();
        range.Contains(end.AddTicks(-1)).ShouldBeTrue();
        range.Contains(end).ShouldBeFalse();
    }

    [TestCase(1)]
    [TestCase(28)]
    [TestCase(31)]
    public void Should_ContainInstantsInsideTheRange_When_DaysIntoTheRangeAreAsked(int days)
    {
        var start = new DateTime(2026, 10, 1, 5, 0, 0);

        var range = new ChurchInstantRange(start, start.AddDays(days));

        range.Contains(start.AddHours(1)).ShouldBeTrue();
        range.Contains(start.AddDays(days).AddTicks(-1)).ShouldBeTrue();
    }

    [Test]
    public void Should_ThrowArgumentException_When_EndIsNotAfterStart()
    {
        var instant = new DateTime(2026, 10, 1, 5, 0, 0);

        Should.Throw<ArgumentException>(() => new ChurchInstantRange(instant, instant));
        Should.Throw<ArgumentException>(() => new ChurchInstantRange(instant, instant.AddSeconds(-1)));
    }
}

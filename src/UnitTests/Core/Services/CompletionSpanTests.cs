using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class CompletionSpanTests
{
    [Test]
    public void Should_DaysToCloseBeElapsedDays_When_BothInstantsPresent()
    {
        var span = new CompletionSpan(new DateTime(2026, 10, 1, 8, 0, 0), new DateTime(2026, 10, 3, 20, 0, 0));

        span.DaysToClose.ShouldBe(2.5);
    }

    [Test]
    public void Should_DaysToCloseBeNull_When_CreatedDateMissing()
    {
        var span = new CompletionSpan(null, new DateTime(2026, 10, 3));

        span.DaysToClose.ShouldBeNull();
    }

    [Test]
    public void Should_DaysToCloseBeNull_When_CompletedDateMissing()
    {
        var span = new CompletionSpan(new DateTime(2026, 10, 1), null);

        span.DaysToClose.ShouldBeNull();
    }
}

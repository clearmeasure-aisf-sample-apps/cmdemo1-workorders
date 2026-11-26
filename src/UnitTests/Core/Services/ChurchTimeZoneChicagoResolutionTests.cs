using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

/// <summary>
/// Pins the America/Chicago resolution so the UTC fallback in <see cref="ChurchTimeZone"/> cannot pass.
/// </summary>
[TestFixture]
public class ChurchTimeZoneChicagoResolutionTests
{
    [Test]
    public void Chicago_Resolved_ShouldNotBeUtcFallback()
    {
        ChurchTimeZone.IsFallback.ShouldBeFalse();
    }

    [Test]
    public void Chicago_Resolved_ShouldHaveBaseUtcOffsetOfMinusSixHours()
    {
        ChurchTimeZone.Chicago.BaseUtcOffset.ShouldBe(TimeSpan.FromHours(-6));
    }

    [Test]
    public void Chicago_JulyInstant_ShouldBeDaylightSavingTime()
    {
        var july = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc);

        ChurchTimeZone.Chicago.IsDaylightSavingTime(july).ShouldBeTrue();
    }

    [Test]
    public void Chicago_JanuaryInstant_ShouldNotBeDaylightSavingTime()
    {
        var january = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc);

        ChurchTimeZone.Chicago.IsDaylightSavingTime(january).ShouldBeFalse();
    }
}

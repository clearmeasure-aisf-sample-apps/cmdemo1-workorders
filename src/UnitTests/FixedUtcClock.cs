namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>Fixed UTC clock for unit tests that need a deterministic <see cref="TimeProvider"/>.</summary>
public sealed class FixedUtcClock(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;

    /// <summary>Creates a clock frozen at <paramref name="utcNow"/>.</summary>
    public static TimeProvider At(DateTimeOffset utcNow) => new FixedUtcClock(utcNow);
}

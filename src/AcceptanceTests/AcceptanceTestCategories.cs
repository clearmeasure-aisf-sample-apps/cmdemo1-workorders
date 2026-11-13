namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// NUnit categories for PR-environment lanes. SerialLane fixtures flip process-wide state
/// (or are not yet independent) and must run after the parallel lane on the same slot.
/// </summary>
public static class AcceptanceTestCategories
{
    /// <summary>Filter: <c>TestCategory==SerialLane</c> / <c>TestCategory!=SerialLane</c>.</summary>
    public const string SerialLane = "SerialLane";
}

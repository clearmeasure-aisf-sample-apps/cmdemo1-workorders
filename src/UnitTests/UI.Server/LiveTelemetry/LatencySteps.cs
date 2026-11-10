using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

/// <summary>
/// Recording steps for the latency and rolling-count fixtures.
/// </summary>
internal static class LatencySteps
{
    public static void AddOneToTwenty(LatencySamples samples, long atMs)
    {
        foreach (var value in Enumerable.Range(1, 20))
        {
            samples.Add(atMs, value);
        }
    }
}

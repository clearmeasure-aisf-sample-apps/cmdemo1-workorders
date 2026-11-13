namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Whether a run may load shared reference data (ZDataLoader) and run the leftover-employee sweep.
/// PR-environment runs hit a pool slot whose template already has reference data; they must not
/// reload or sweep. Local runs that start UI.Server still seed once.
/// </summary>
public static class AcceptanceSharedDataPolicy
{
    /// <summary>
    /// Returns true only when the suite starts a local server (classic CI / laptop).
    /// Remote ApplicationBaseUrl runs (pool slots) return false.
    /// </summary>
    public static bool ShouldLoadSharedData(bool startLocalServer) => startLocalServer;
}

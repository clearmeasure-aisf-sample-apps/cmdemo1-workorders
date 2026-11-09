namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Solution scan: unused packages and projects whose build output was missing.
/// </summary>
public sealed record UnusedPackageScan(
    IReadOnlyList<UnusedPackage> Unused,
    IReadOnlyList<string> MissingOutputs);

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// How an <see cref="ExternalCommand"/> ended.
/// </summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="Output">Standard output followed by standard error.</param>
public sealed record CommandOutcome(int ExitCode, string Output);

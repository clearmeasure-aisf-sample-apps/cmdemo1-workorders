namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// One production method over the CRAP threshold.
/// </summary>
/// <param name="FullName">Fully qualified method name.</param>
/// <param name="FilePath">Source path as the report gives it.</param>
/// <param name="Score">The method's CRAP measurements.</param>
public sealed record CrapMethodViolation(string FullName, string FilePath, CrapScore Score);

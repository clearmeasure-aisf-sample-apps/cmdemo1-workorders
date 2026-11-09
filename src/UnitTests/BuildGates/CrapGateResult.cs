namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Outcome of a CRAP production-scope gate evaluation.
/// </summary>
/// <param name="Threshold">Score a production method may reach without failing the gate.</param>
/// <param name="ViolationCount">Production methods over <paramref name="Threshold"/>.</param>
/// <param name="Methods">Those methods, highest score first.</param>
public sealed record CrapGateResult(int Threshold, int ViolationCount, IReadOnlyList<CrapMethodViolation> Methods);

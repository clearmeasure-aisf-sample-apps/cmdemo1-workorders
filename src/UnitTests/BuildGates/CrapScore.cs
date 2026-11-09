namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The measurements crap4dotnet reports for one method.
/// </summary>
/// <param name="Crap">CRAP score: complexity squared times uncovered share cubed, plus complexity.</param>
/// <param name="Complexity">Cyclomatic complexity.</param>
/// <param name="Coverage">Line coverage in percent.</param>
public sealed record CrapScore(double Crap, int Complexity, double Coverage);

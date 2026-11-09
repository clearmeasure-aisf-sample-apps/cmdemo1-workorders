namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// A type or method that breaks a structure rule. <see cref="Key"/> is the line written to a ratchet baseline.
/// </summary>
public sealed record StructureViolation(string RelativePath, string Symbol)
{
    public string Key => $"{RelativePath} :: {Symbol}";
}

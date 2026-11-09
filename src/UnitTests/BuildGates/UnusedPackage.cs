namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// One direct package whose assemblies are not referenced by the built project.
/// </summary>
public sealed record UnusedPackage(string Project, string PackageId);

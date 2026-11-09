namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Deterministic scan for superfluous structure: single-implementation interfaces, single-subclass abstract
/// classes, pass-through classes, single-file folders, folders nested deeper than four under <c>src/</c>,
/// and library projects nothing references.
/// </summary>
public static class SuperfluousStructureScanner
{
    /// <summary>
    /// Scans <paramref name="repositoryRoot"/> (the directory that contains <c>src/</c>).
    /// </summary>
    public static StructureReport Scan(string repositoryRoot)
    {
        var types = SourceTypeParser.ParseRepository(repositoryRoot);
        return new StructureReport
        {
            SingleImplementationInterfaces = ImplementationCounter.SingleImplementationInterfaces(types),
            SingleSubclassAbstractClasses = ImplementationCounter.SingleSubclassAbstractClasses(types),
            PassThroughClasses = PassThroughClasses(types),
            SingleFileFolders = FolderShapeInspector.SingleFileFolders(repositoryRoot),
            DeepFolders = FolderShapeInspector.DeepFolders(repositoryRoot),
            UnreferencedProjects = ProjectReferenceInspector.UnreferencedProjects(repositoryRoot),
        };
    }

    private static IReadOnlyList<string> PassThroughClasses(IReadOnlyList<ParsedType> types)
    {
        return types
            .Where(type => type.SawForwarder && !type.PassThroughDisqualified && type.ForwardTarget != null)
            .Select(type => type.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }
}

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Deterministic scan for superfluous structure: single-implementation interfaces, single-subclass abstract
/// classes, pass-through classes, single-file folders, folders nested deeper than four under <c>src/</c>,
/// and library projects nothing references.
/// </summary>
public static class SuperfluousStructureScanner
{
    /// <summary>
    /// Scans the <c>src/</c> tree of <paramref name="checkout"/>.
    /// </summary>
    public static StructureReport Scan(RepositoryCheckout checkout)
    {
        var types = SourceTypeParser.ParseRepository(checkout);
        return new StructureReport
        {
            SingleImplementationInterfaces = ImplementationCounter.SingleImplementationInterfaces(types),
            SingleSubclassAbstractClasses = ImplementationCounter.SingleSubclassAbstractClasses(types),
            PassThroughClasses = PassThroughClasses(types),
            SingleFileFolders = FolderShapeInspector.SingleFileFolders(checkout),
            DeepFolders = FolderShapeInspector.DeepFolders(checkout),
            UnreferencedProjects = ProjectReferenceInspector.UnreferencedProjects(checkout),
        };
    }

    private static IReadOnlyList<string> PassThroughClasses(IReadOnlyList<ParsedType> types)
    {
        return types
            .Where(type => type is { SawForwarder: true, PassThroughDisqualified: false, ForwardTarget: not null })
            .Select(type => type.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }
}

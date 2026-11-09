namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Stable keys for the superfluous-structure rules. Allowlist and baseline files use the same names.
/// </summary>
public static class StructureRule
{
    /// <summary>Interfaces with exactly one concrete implementation.</summary>
    public const string SingleImplementationInterface = "singleImplementationInterfaces";

    /// <summary>Abstract classes or records with exactly one direct subclass.</summary>
    public const string SingleSubclassAbstractClass = "singleSubclassAbstractClasses";

    /// <summary>Classes whose members only forward to one other member.</summary>
    public const string PassThroughClass = "passThroughClasses";

    /// <summary>Folders that contain a single source file and no subfolders.</summary>
    public const string SingleFileFolder = "singleFileFolders";

    /// <summary>Folders nested more than four levels below <c>src/</c>.</summary>
    public const string DeepFolder = "deepFolders";

    /// <summary>Library projects that no other project references.</summary>
    public const string UnreferencedProject = "unreferencedProjects";

    /// <summary>Every rule key, in baseline-file order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SingleImplementationInterface,
        SingleSubclassAbstractClass,
        PassThroughClass,
        SingleFileFolder,
        DeepFolder,
        UnreferencedProject
    ];
}

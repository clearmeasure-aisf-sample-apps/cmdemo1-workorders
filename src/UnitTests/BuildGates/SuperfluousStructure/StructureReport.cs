namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Raw findings from one repository scan, before allowlist exemptions.
/// </summary>
public sealed class StructureReport
{
    /// <summary>Interfaces with exactly one concrete implementation.</summary>
    public IReadOnlyList<string> SingleImplementationInterfaces { get; init; } = [];

    /// <summary>Abstract classes or records with exactly one direct subclass.</summary>
    public IReadOnlyList<string> SingleSubclassAbstractClasses { get; init; } = [];

    /// <summary>Classes whose members only forward to one other member.</summary>
    public IReadOnlyList<string> PassThroughClasses { get; init; } = [];

    /// <summary>Folders that contain a single source file and no subfolders.</summary>
    public IReadOnlyList<string> SingleFileFolders { get; init; } = [];

    /// <summary>Folders nested more than four levels below <c>src/</c>.</summary>
    public IReadOnlyList<string> DeepFolders { get; init; } = [];

    /// <summary>Library projects that no other project references.</summary>
    public IReadOnlyList<string> UnreferencedProjects { get; init; } = [];

    /// <summary>
    /// Findings for <paramref name="rule"/>. The rule must be one of <see cref="StructureRule.All"/>.
    /// </summary>
    public IReadOnlyList<string> Findings(string rule)
    {
        if (!Lists().TryGetValue(rule, out var findings))
        {
            throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown structure rule.");
        }

        return findings;
    }

    private Dictionary<string, IReadOnlyList<string>> Lists()
    {
        return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [StructureRule.SingleImplementationInterface] = SingleImplementationInterfaces,
            [StructureRule.SingleSubclassAbstractClass] = SingleSubclassAbstractClasses,
            [StructureRule.PassThroughClass] = PassThroughClasses,
            [StructureRule.SingleFileFolder] = SingleFileFolders,
            [StructureRule.DeepFolder] = DeepFolders,
            [StructureRule.UnreferencedProject] = UnreferencedProjects,
        };
    }
}

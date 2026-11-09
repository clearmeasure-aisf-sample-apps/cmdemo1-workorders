namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Raw findings from one repository scan, before allowlist exemptions.
/// </summary>
public sealed record StructureReport(
    IReadOnlyList<string> SingleImplementationInterfaces,
    IReadOnlyList<string> SingleSubclassAbstractClasses,
    IReadOnlyList<string> PassThroughClasses,
    IReadOnlyList<string> SingleFileFolders,
    IReadOnlyList<string> DeepFolders,
    IReadOnlyList<string> UnreferencedProjects)
{
    /// <summary>
    /// Findings for <paramref name="rule"/>. The rule must be one of <see cref="StructureRule.All"/>.
    /// </summary>
    public IReadOnlyList<string> Findings(string rule)
    {
        if (rule == StructureRule.SingleImplementationInterface)
        {
            return SingleImplementationInterfaces;
        }

        if (rule == StructureRule.SingleSubclassAbstractClass)
        {
            return SingleSubclassAbstractClasses;
        }

        if (rule == StructureRule.PassThroughClass)
        {
            return PassThroughClasses;
        }

        if (rule == StructureRule.SingleFileFolder)
        {
            return SingleFileFolders;
        }

        if (rule == StructureRule.DeepFolder)
        {
            return DeepFolders;
        }

        if (rule == StructureRule.UnreferencedProject)
        {
            return UnreferencedProjects;
        }

        throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown structure rule.");
    }
}

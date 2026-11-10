namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Builds a small <see cref="SourceSet"/> of sample files for the structure-rule fixtures.
/// </summary>
public sealed class SourceSetBuilder
{
    private readonly List<SourceFile> _files = [];
    private readonly List<string> _razorComponents = [];

    public SourceSetBuilder WithFile(string relativePath, string code)
    {
        _files.Add(SourceFile.Parse(relativePath, code));
        return this;
    }

    public SourceSetBuilder WithRazorComponent(string relativePath)
    {
        _razorComponents.Add(relativePath);
        return this;
    }

    public SourceSet Build() => SourceSet.FromSamples(_files, _razorComponents.ToArray());

    public static IEnumerable<string> KeysOf(IStructureRule rule, SourceSet sources) =>
        rule.FindViolations(sources).Select(violation => violation.Key);
}

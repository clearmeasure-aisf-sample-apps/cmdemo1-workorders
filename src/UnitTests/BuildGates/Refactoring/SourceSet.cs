namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// The hand-written C# files under <c>src/</c> (generated code and build output excluded) and the paths of the
/// Razor components next to them.
/// </summary>
public sealed class SourceSet
{
    private SourceSet(IReadOnlyList<SourceFile> files, IEnumerable<string> razorComponents)
    {
        Files = files;
        RazorComponents = razorComponents
            .Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        DeclaredTypeNames = files
            .SelectMany(file => file.TypeDeclarations)
            .Select(type => type.Identifier.Text)
            .ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyList<SourceFile> Files { get; }

    public IReadOnlySet<string> RazorComponents { get; }

    public IReadOnlySet<string> DeclaredTypeNames { get; }

    public static SourceSet FromRepository(RepositoryCheckout checkout)
    {
        var files = checkout.SourceFiles(".cs")
            .Select(path => SourceFile.Parse(checkout.Relative(path), File.ReadAllText(path)))
            .ToList();
        return new SourceSet(files, checkout.SourceFiles(".razor").Select(checkout.Relative));
    }

    public static SourceSet FromSamples(IEnumerable<SourceFile> files, params string[] razorComponents) =>
        new(files.ToList(), razorComponents);
}

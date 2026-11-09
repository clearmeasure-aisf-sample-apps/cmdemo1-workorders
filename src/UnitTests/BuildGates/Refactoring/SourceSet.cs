namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// The hand-written C# files under <c>src/</c> (generated code and build output excluded) and the paths of the
/// Razor components next to them.
/// </summary>
public sealed class SourceSet
{
    private static readonly string[] ExcludedDirectories = ["bin", "obj", "Generated", "node_modules"];

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

    public static SourceSet FromRepository(string repositoryRoot)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var files = EnumerateHandWritten(sourceRoot, "*.cs")
            .Where(path => !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => SourceFile.Parse(Path.GetRelativePath(repositoryRoot, path), File.ReadAllText(path)))
            .ToList();
        var razorComponents = EnumerateHandWritten(sourceRoot, "*.razor")
            .Select(path => Path.GetRelativePath(repositoryRoot, path));
        return new SourceSet(files, razorComponents);
    }

    public static SourceSet FromSamples(IEnumerable<SourceFile> files, params string[] razorComponents) =>
        new(files.ToList(), razorComponents);

    private static IEnumerable<string> EnumerateHandWritten(string sourceRoot, string pattern) =>
        Directory.EnumerateFiles(sourceRoot, pattern, SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(sourceRoot, path)
                .Split(Path.DirectorySeparatorChar)
                .Any(segment => ExcludedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase)));
}

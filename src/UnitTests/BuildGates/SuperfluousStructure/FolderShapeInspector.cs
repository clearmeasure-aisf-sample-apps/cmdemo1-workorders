namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Flags folders that hold one source file, and folders nested more than four levels under <c>src/</c>.
/// </summary>
internal static class FolderShapeInspector
{
    private const int MaxDepth = 4;

    private static readonly string[] SourceExtensions = [".cs", ".razor", ".cshtml"];

    public static IReadOnlyList<string> SingleFileFolders(RepositoryCheckout checkout)
    {
        return Scan(checkout).SingleFile;
    }

    public static IReadOnlyList<string> DeepFolders(RepositoryCheckout checkout)
    {
        return Scan(checkout).Deep;
    }

    private static FolderFindings Scan(RepositoryCheckout checkout)
    {
        var findings = new FolderFindings();
        var src = checkout.PathOf("src");
        if (Directory.Exists(src))
        {
            Walk(src, checkout, src, findings);
            findings.SingleFile.Sort(StringComparer.Ordinal);
            findings.Deep.Sort(StringComparer.Ordinal);
        }

        return findings;
    }

    private static void Walk(string directory, RepositoryCheckout checkout, string src, FolderFindings findings)
    {
        var children = ChildDirectories(directory);
        var sources = Directory.GetFiles(directory).Where(IsSourceFile).ToArray();
        if (sources.Length == 1 && children.Length == 0)
        {
            findings.SingleFile.Add(checkout.Relative(directory));
        }

        if (Depth(src, directory) > MaxDepth && sources.Length > 0)
        {
            findings.Deep.Add(checkout.Relative(directory));
        }

        foreach (var child in children)
        {
            Walk(child, checkout, src, findings);
        }
    }

    private static bool IsSourceFile(string path)
    {
        return SourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static string[] ChildDirectories(string directory)
    {
        return Directory.GetDirectories(directory)
            .Where(path => !RepositoryCheckout.IsSkippedDirectory(Path.GetFileName(path)))
            .ToArray();
    }

    private static int Depth(string src, string directory)
    {
        var relative = Path.GetRelativePath(src, directory);
        if (relative == ".")
        {
            return 0;
        }

        return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
    }

    private sealed class FolderFindings
    {
        public List<string> SingleFile { get; } = [];

        public List<string> Deep { get; } = [];
    }
}

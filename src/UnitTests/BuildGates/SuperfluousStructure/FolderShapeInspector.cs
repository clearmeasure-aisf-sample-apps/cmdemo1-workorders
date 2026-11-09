namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Flags folders that hold one source file, and folders nested more than four levels under <c>src/</c>.
/// </summary>
internal static class FolderShapeInspector
{
    private const int MaxDepth = 4;

    public static IReadOnlyList<string> SingleFileFolders(string repositoryRoot)
    {
        return Scan(repositoryRoot).SingleFile;
    }

    public static IReadOnlyList<string> DeepFolders(string repositoryRoot)
    {
        return Scan(repositoryRoot).Deep;
    }

    private static FolderFindings Scan(string repositoryRoot)
    {
        var findings = new FolderFindings();
        var src = Path.Combine(repositoryRoot, "src");
        if (Directory.Exists(src))
        {
            Walk(src, repositoryRoot, src, findings);
            findings.SingleFile.Sort(StringComparer.Ordinal);
            findings.Deep.Sort(StringComparer.Ordinal);
        }

        return findings;
    }

    private static void Walk(string directory, string root, string src, FolderFindings findings)
    {
        var children = ChildDirectories(directory);
        var sources = Directory.GetFiles(directory).Where(StructurePaths.IsSourceFile).ToArray();
        if (sources.Length == 1 && children.Length == 0)
        {
            findings.SingleFile.Add(StructurePaths.Relative(root, directory));
        }

        if (Depth(src, directory) > MaxDepth && sources.Length > 0)
        {
            findings.Deep.Add(StructurePaths.Relative(root, directory));
        }

        foreach (var child in children)
        {
            Walk(child, root, src, findings);
        }
    }

    private static string[] ChildDirectories(string directory)
    {
        return Directory.GetDirectories(directory)
            .Where(path => !StructurePaths.IsSkippedDirectory(Path.GetFileName(path)))
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

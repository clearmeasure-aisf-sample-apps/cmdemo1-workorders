namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Path filters shared by the source scan and the folder scan.
/// </summary>
internal static class StructurePaths
{
    public static bool IsSkippedDirectory(string name)
    {
        return name.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Generated", StringComparison.OrdinalIgnoreCase)
            || name.Equals("TestResults", StringComparison.OrdinalIgnoreCase)
            || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSourceFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsParsedCSharp(string path)
    {
        if (!Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = Path.GetFileName(path);
        return !name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            && !name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    public static string Relative(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }
}

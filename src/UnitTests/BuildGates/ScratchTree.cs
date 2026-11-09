namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A throwaway directory tree under the temp directory that a gate test writes files into,
/// deleted with everything in it on dispose, read-only files included.
/// </summary>
public sealed class ScratchTree : IDisposable
{
    /// <summary>Creates an empty tree whose folder name starts with <paramref name="purpose"/>.</summary>
    public ScratchTree(string purpose)
    {
        Root = Path.Combine(Path.GetTempPath(), $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>Absolute path of the tree.</summary>
    public string Root { get; }

    /// <summary>The tree seen as a repository checkout.</summary>
    public RepositoryCheckout Checkout => RepositoryCheckout.At(Root);

    /// <summary>Absolute path of <paramref name="relativePath"/>, a <c>/</c>-separated path in the tree.</summary>
    public string PathOf(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Writes <paramref name="contents"/> to <paramref name="relativePath"/>, creating its folders.</summary>
    public ScratchTree Write(string relativePath, string contents)
    {
        var path = PathOf(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return this;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(Root, recursive: true);
    }
}

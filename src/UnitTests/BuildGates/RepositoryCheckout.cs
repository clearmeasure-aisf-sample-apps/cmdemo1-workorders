namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The working copy a build gate inspects: where it is, what its files say, which sources are
/// hand-written, and the ratchet baselines committed in it. Paths given to and returned by this
/// type are relative to <see cref="Root"/> and use <c>/</c>.
/// </summary>
public sealed class RepositoryCheckout
{
    private const string SolutionMarker = "src/ChurchBulletin.sln";

    private static readonly Lazy<RepositoryCheckout> CurrentCheckout = new(FindFromTestDirectory);

    private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "Generated", "TestResults", "node_modules",
    };

    private static readonly string[] GeneratedSuffixes = [".g.cs", ".designer.cs"];

    private RepositoryCheckout(string root)
    {
        Root = root;
    }

    /// <summary>
    /// The checkout that contains the running test assembly, found by walking up to the solution.
    /// </summary>
    public static RepositoryCheckout Current => CurrentCheckout.Value;

    /// <summary>Absolute path of the checkout root.</summary>
    public string Root { get; }

    /// <summary>A checkout rooted at <paramref name="root"/>, such as a fabricated tree in a test.</summary>
    public static RepositoryCheckout At(string root)
    {
        return new RepositoryCheckout(Path.GetFullPath(root));
    }

    /// <summary>
    /// True when a directory with this name holds build output, generated code, or packages.
    /// </summary>
    public static bool IsSkippedDirectory(string directoryName)
    {
        return SkippedDirectories.Contains(directoryName);
    }

    /// <summary>
    /// True for a checkout-relative path that no skipped directory contains and no generator wrote.
    /// </summary>
    public static bool IsHandWritten(string relativePath)
    {
        var folders = relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
        return !folders.Any(IsSkippedDirectory) && !IsGeneratedFile(relativePath);
    }

    /// <summary>Absolute path of <paramref name="relativePath"/>.</summary>
    public string PathOf(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary><paramref name="fullPath"/> relative to <see cref="Root"/>, with <c>/</c> separators.</summary>
    public string Relative(string fullPath)
    {
        return Path.GetRelativePath(Root, fullPath).Replace('\\', '/');
    }

    /// <summary>Text of a committed file. A missing file fails with its relative path.</summary>
    public string ReadText(string relativePath)
    {
        var path = PathOf(relativePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{relativePath} was not found in the checkout {Root}.", path);
        }

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Hand-written files under <c>src/</c> with one of <paramref name="extensions"/>, in ordinal path order.
    /// A checkout without <c>src/</c> has none.
    /// </summary>
    public IReadOnlyList<string> SourceFiles(params string[] extensions)
    {
        var files = new List<string>();
        var src = PathOf("src");
        var pending = new Stack<string>(Directory.Exists(src) ? [src] : []);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            files.AddRange(Directory.GetFiles(directory).Where(file => IsSource(file, extensions)));
            foreach (var child in Directory.GetDirectories(directory))
            {
                if (!IsSkippedDirectory(Path.GetFileName(child)))
                {
                    pending.Push(child);
                }
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    /// <summary>Loads the committed key ratchet at <paramref name="relativePath"/>.</summary>
    public KeyRatchet ReadRatchet(string relativePath)
    {
        return KeyRatchet.Parse(ReadText(relativePath));
    }

    private static bool IsSource(string file, string[] extensions)
    {
        return extensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            && !IsGeneratedFile(file);
    }

    private static bool IsGeneratedFile(string path)
    {
        return GeneratedSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static RepositoryCheckout FindFromTestDirectory()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, SolutionMarker)))
        {
            directory = directory.Parent;
        }

        return directory == null
            ? throw new DirectoryNotFoundException($"No {SolutionMarker} above {TestContext.CurrentContext.TestDirectory}.")
            : new RepositoryCheckout(directory.FullName);
    }
}

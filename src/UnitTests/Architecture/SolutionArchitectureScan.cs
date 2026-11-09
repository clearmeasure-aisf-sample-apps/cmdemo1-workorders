using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Reads the solution on disk and applies <see cref="ArchitectureRules"/>.
/// Generated code, <c>bin</c>, and <c>obj</c> are out of scope.
/// </summary>
internal static class SolutionArchitectureScan
{
    private static readonly char[] FolderSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>
    /// Findings for the five architecture rules against <paramref name="repoRoot"/>.
    /// </summary>
    internal static Result Scan(string repoRoot)
    {
        var (coreProjectReferences, uiDataAccessReferences) = ScanProjects(repoRoot);
        var (handlerNamingViolations, multiplePublicTypeFiles, namespaceMismatches) = ScanSources(repoRoot);
        return new Result
        {
            CoreProjectReferences = coreProjectReferences,
            UiDataAccessReferences = uiDataAccessReferences,
            HandlerNamingViolations = handlerNamingViolations,
            MultiplePublicTypeFiles = multiplePublicTypeFiles,
            NamespaceMismatches = namespaceMismatches,
        };
    }

    /// <summary>
    /// Repository root, found by walking up from the test output directory.
    /// </summary>
    internal static string FindRepoRoot()
    {
        return RepoFiles.Root();
    }

    /// <summary>
    /// Reads <c>src/UnitTests/Architecture/Baselines/{fileName}</c>.
    /// </summary>
    internal static IReadOnlySet<string> ReadBaseline(string repoRoot, string fileName)
    {
        var path = Path.Combine(repoRoot, "src", "UnitTests", "Architecture", "Baselines", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Architecture baseline was not found.", path);
        }

        return ArchitectureRules.ParseBaseline(File.ReadAllText(path));
    }

    /// <summary>
    /// True for generated source, designer files, and build output. Those paths are not architecture-gated.
    /// </summary>
    internal static bool IsExcludedSource(string path)
    {
        var normalized = path.Replace('\\', '/');
        if (normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.Contains("/Generated/", StringComparison.Ordinal))
        {
            return true;
        }

        return normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static (IReadOnlyList<string> Core, IReadOnlyList<string> UiDataAccess) ScanProjects(string repoRoot)
    {
        var core = new List<string>();
        var uiDataAccess = new List<string>();
        foreach (var projectFile in ProjectFiles(repoRoot))
        {
            var relative = RelativePath(repoRoot, projectFile);
            var xml = File.ReadAllText(projectFile);
            core.AddRange(ArchitectureRules.CoreProjectReferenceViolations(relative, xml));
            if (ArchitectureRules.IsUiProject(relative) && ArchitectureRules.ReferencesDataAccess(xml))
            {
                uiDataAccess.Add(relative);
            }
        }

        return (Sorted(core), Sorted(uiDataAccess));
    }

    private static (
        IReadOnlyList<string> Handlers,
        IReadOnlyList<string> MultiplePublicTypes,
        IReadOnlyList<string> NamespaceMismatches) ScanSources(string repoRoot)
    {
        var handlers = new List<string>();
        var multiplePublicTypes = new List<string>();
        var namespaceMismatches = new List<string>();
        var projects = new Dictionary<string, (string RootNamespace, string Directory)>(StringComparer.Ordinal);
        foreach (var sourceFile in SourceFiles(repoRoot))
        {
            var relative = RelativePath(repoRoot, sourceFile);
            var root = ArchitectureRules.Parse(File.ReadAllText(sourceFile));
            foreach (var name in ArchitectureRules.MisnamedHandlerTypeNames(root))
            {
                handlers.Add(relative + ":" + name);
            }
            if (ArchitectureRules.PublicTopLevelTypeNames(root).Count > 1)
            {
                multiplePublicTypes.Add(relative);
            }

            if (NamespaceMismatch(sourceFile, root, projects))
            {
                namespaceMismatches.Add(relative);
            }
        }

        return (Sorted(handlers), Sorted(multiplePublicTypes), Sorted(namespaceMismatches));
    }

    private static bool NamespaceMismatch(
        string sourceFile,
        CompilationUnitSyntax root,
        Dictionary<string, (string RootNamespace, string Directory)> projects)
    {
        var declared = ArchitectureRules.DeclaredNamespaces(root);
        if (declared.Count == 0 || !TryProject(sourceFile, projects, out var info))
        {
            return false;
        }

        return DeclaredNamespaceDiffers(sourceFile, declared, info);
    }

    private static bool TryProject(
        string sourceFile,
        Dictionary<string, (string RootNamespace, string Directory)> projects,
        out (string RootNamespace, string Directory) info)
    {
        info = default;
        var projectFile = FindProjectFile(sourceFile);
        if (projectFile == null)
        {
            return false;
        }

        if (projects.TryGetValue(projectFile, out info))
        {
            return true;
        }

        var projectDirectory = Path.GetDirectoryName(projectFile);
        if (projectDirectory == null)
        {
            return false;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        info = (ArchitectureRules.RootNamespace(File.ReadAllText(projectFile), projectName), projectDirectory);
        projects[projectFile] = info;
        return true;
    }

    private static bool DeclaredNamespaceDiffers(
        string sourceFile,
        IReadOnlyList<string> declared,
        (string RootNamespace, string Directory) info)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceFile);
        if (sourceDirectory == null)
        {
            return false;
        }

        var folder = Path.GetRelativePath(info.Directory, sourceDirectory);
        var segments = folder == "." ? [] : folder.Split(FolderSeparators, StringSplitOptions.RemoveEmptyEntries);
        var expected = ArchitectureRules.ExpectedNamespace(info.RootNamespace, segments);
        foreach (var ns in declared)
        {
            if (ns != expected)
            {
                return true;
            }
        }

        return false;
    }

    private static string? FindProjectFile(string sourceFile)
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile) ?? sourceFile);
        while (dir != null)
        {
            var matches = dir.GetFiles("*.csproj");
            if (matches.Length == 1)
            {
                return matches[0].FullName;
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"More than one project file in {dir.FullName}.");
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static IEnumerable<string> ProjectFiles(string repoRoot)
    {
        return EnumerateUnderSrc(repoRoot, "*.csproj");
    }

    private static IEnumerable<string> SourceFiles(string repoRoot)
    {
        return EnumerateUnderSrc(repoRoot, "*.cs");
    }

    private static IEnumerable<string> EnumerateUnderSrc(string repoRoot, string pattern)
    {
        var src = Path.Combine(repoRoot, "src");
        foreach (var file in Directory.EnumerateFiles(src, pattern, SearchOption.AllDirectories))
        {
            if (!IsExcludedSource(file))
            {
                yield return file;
            }
        }
    }

    private static string RelativePath(string repoRoot, string fullPath)
    {
        return Path.GetRelativePath(repoRoot, fullPath).Replace('\\', '/');
    }

    private static IReadOnlyList<string> Sorted(List<string> items)
    {
        var set = new SortedSet<string>(items, StringComparer.Ordinal);
        return new List<string>(set);
    }

    /// <summary>
    /// Architecture findings for one scan of the repository.
    /// </summary>
    internal sealed class Result
    {
        public IReadOnlyList<string> CoreProjectReferences { get; init; } = [];

        public IReadOnlyList<string> UiDataAccessReferences { get; init; } = [];

        public IReadOnlyList<string> HandlerNamingViolations { get; init; } = [];

        public IReadOnlyList<string> MultiplePublicTypeFiles { get; init; } = [];

        public IReadOnlyList<string> NamespaceMismatches { get; init; } = [];
    }
}

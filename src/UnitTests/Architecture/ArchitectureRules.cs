using System.Xml.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic architecture checks for the teaching solution.
/// Core references no other solution project. UI projects do not reference DataAccess
/// beyond the shrinking allowlist. MediatR handler types end in <c>Handler</c>.
/// A source file declares at most one public top-level type. A file's namespace
/// matches its project root namespace plus its folder path.
/// </summary>
internal static partial class ArchitectureRules
{
    private const string DataAccessProjectFile = "DataAccess.csproj";
    private const string CoreProjectPath = "src/Core/Core.csproj";
    private const string ProjectNameToken = "$(MSBuildProjectName)";
    private const string ProjectNameWithUnderscoresToken = "$(MSBuildProjectName.Replace(\" \", \"_\"))";

    /// <summary>
    /// ProjectReference include paths in a project file, using forward slashes.
    /// </summary>
    private static IReadOnlyList<string> ProjectReferenceIncludes(string projectXml)
    {
        var document = XDocument.Parse(StripBom(projectXml));
        var includes = new List<string>();
        foreach (var element in document.Descendants())
        {
            if (element.Name.LocalName != "ProjectReference")
            {
                continue;
            }

            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            includes.Add(NormalizeSlashes(include.Trim()));
        }

        return SortedDistinct(includes);
    }

    /// <summary>
    /// True when <paramref name="relativeProjectPath"/> is <c>src/Core/Core.csproj</c>.
    /// </summary>
    private static bool IsCoreProject(string relativeProjectPath)
    {
        return string.Equals(NormalizeSlashes(relativeProjectPath), CoreProjectPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Solution projects referenced by Core. Empty when the path is not Core or Core has no project references.
    /// </summary>
    internal static IReadOnlyList<string> CoreProjectReferenceViolations(string relativeProjectPath, string projectXml)
    {
        if (!IsCoreProject(relativeProjectPath))
        {
            return [];
        }

        return ProjectReferenceIncludes(projectXml);
    }

    /// <summary>
    /// True when the project lives under <c>src/UI/</c> or <c>src/UI.Shared/</c>.
    /// </summary>
    internal static bool IsUiProject(string relativeProjectPath)
    {
        var path = NormalizeSlashes(relativeProjectPath);
        return path.StartsWith("src/UI/", StringComparison.Ordinal)
            || path.StartsWith("src/UI.Shared/", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when the project file references <c>DataAccess.csproj</c>.
    /// </summary>
    internal static bool ReferencesDataAccess(string projectXml)
    {
        foreach (var include in ProjectReferenceIncludes(projectXml))
        {
            if (string.Equals(ProjectFileName(include), DataAccessProjectFile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// UI projects in <paramref name="projects"/> that reference DataAccess.
    /// </summary>
    internal static IReadOnlyList<string> UiProjectsReferencingDataAccess(
        IEnumerable<(string RelativeProjectPath, string ProjectXml)> projects)
    {
        var violations = new List<string>();
        foreach (var (relativeProjectPath, projectXml) in projects)
        {
            if (IsUiProject(relativeProjectPath) && ReferencesDataAccess(projectXml))
            {
                violations.Add(NormalizeSlashes(relativeProjectPath));
            }
        }

        return SortedDistinct(violations);
    }

    /// <summary>
    /// Expands <c>RootNamespace</c> from a project file. Missing values use <paramref name="projectName"/>.
    /// </summary>
    internal static string RootNamespace(string projectXml, string projectName)
    {
        var document = XDocument.Parse(StripBom(projectXml));
        foreach (var element in document.Descendants())
        {
            if (element.Name.LocalName != "RootNamespace")
            {
                continue;
            }

            var value = element.Value.Trim();
            if (value.Length > 0)
            {
                return ExpandRootNamespace(value, projectName);
            }
        }

        return projectName;
    }

    /// <summary>
    /// Root namespace plus each folder under the project directory.
    /// </summary>
    internal static string ExpectedNamespace(string rootNamespace, IReadOnlyList<string> folderSegments)
    {
        var parts = new List<string> { rootNamespace };
        foreach (var segment in folderSegments)
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            parts.Add(segment);
        }

        return string.Join('.', parts);
    }

    private static string ExpandRootNamespace(string value, string projectName)
    {
        var withUnderscores = projectName.Replace(" ", "_", StringComparison.Ordinal);
        var expanded = value.Replace(ProjectNameWithUnderscoresToken, withUnderscores, StringComparison.Ordinal);
        return expanded.Replace(ProjectNameToken, projectName, StringComparison.Ordinal);
    }

    private static string ProjectFileName(string include)
    {
        var slash = include.LastIndexOf('/');
        return slash < 0 ? include : include[(slash + 1)..];
    }

    private static string NormalizeSlashes(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string StripBom(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            return text[1..];
        }

        return text;
    }

    private static IReadOnlyList<string> SortedDistinct(IEnumerable<string> items)
    {
        var set = new SortedSet<string>(items, StringComparer.Ordinal);
        return new List<string>(set);
    }
}

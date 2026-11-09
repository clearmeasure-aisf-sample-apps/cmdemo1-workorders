using System.Xml.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Flags library projects under <c>src/</c> that nothing references.
/// Hosts (Exe, Worker SDK, AppHost SDK) and the test projects are excluded.
/// </summary>
internal static class ProjectReferenceInspector
{
    public static IReadOnlyList<string> UnreferencedProjects(RepositoryCheckout checkout)
    {
        var projects = checkout.SourceFiles(".csproj").Select(file => ProjectFile.Read(file, checkout)).ToList();
        var referenced = ReferencedProjects(checkout.Root, projects);
        return projects
            .Where(project => !project.IsExcluded && !referenced.Contains(project.FullPath))
            .Select(project => project.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static HashSet<string> ReferencedProjects(string repositoryRoot, IReadOnlyList<ProjectFile> projects)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in ReferenceFiles(repositoryRoot, projects))
        {
            foreach (var reference in ReadReferences(file))
            {
                referenced.Add(reference);
            }
        }

        return referenced;
    }

    private static List<string> ReferenceFiles(string repositoryRoot, IReadOnlyList<ProjectFile> projects)
    {
        var files = projects.Select(project => project.FullPath).ToList();
        foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets" })
        {
            var rooted = Path.Combine(repositoryRoot, name);
            var underSrc = Path.Combine(repositoryRoot, "src", name);
            if (File.Exists(rooted))
            {
                files.Add(rooted);
            }

            if (File.Exists(underSrc))
            {
                files.Add(underSrc);
            }
        }

        return files;
    }

    private static IEnumerable<string> ReadReferences(string filePath)
    {
        var document = XDocument.Load(filePath);
        var properties = ReadProperties(document, filePath);
        foreach (var reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
        {
            var include = reference.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var expanded = Expand(include.Trim(), properties).Replace('\\', Path.DirectorySeparatorChar);
            if (expanded.Contains("$(", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Unresolved MSBuild property in ProjectReference '{include}' from '{filePath}'.");
            }

            var directory = Path.GetDirectoryName(filePath)!;
            yield return Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(directory, expanded));
        }
    }

    private static Dictionary<string, string> ReadProperties(XDocument document, string filePath)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MSBuildThisFile"] = filePath,
            ["MSBuildThisFileDirectory"] = Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar
        };

        foreach (var element in document.Descendants())
        {
            if (element.Parent?.Name.LocalName != "PropertyGroup" || element.HasElements)
            {
                continue;
            }

            properties[element.Name.LocalName] = element.Value.Trim();
        }

        return properties;
    }

    private static string Expand(string value, IReadOnlyDictionary<string, string> properties)
    {
        var current = value;
        for (var pass = 0; pass < 8; pass++)
        {
            var next = ReplaceProperties(current, properties);
            if (next == current)
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    private static string ReplaceProperties(string value, IReadOnlyDictionary<string, string> properties)
    {
        var current = value;
        foreach (var property in properties)
        {
            current = current.Replace("$(" + property.Key + ")", property.Value, StringComparison.OrdinalIgnoreCase);
        }

        return current;
    }

    private sealed class ProjectFile
    {
        public string FullPath { get; private init; } = "";

        public string RelativePath { get; private init; } = "";

        public bool IsExcluded { get; private init; }

        public static ProjectFile Read(string fullPath, RepositoryCheckout checkout)
        {
            var document = XDocument.Load(fullPath);
            return new ProjectFile
            {
                FullPath = Path.GetFullPath(fullPath),
                RelativePath = checkout.Relative(fullPath),
                IsExcluded = IsHost(document) || IsTestProject(fullPath)
            };
        }

        private static bool IsTestProject(string fullPath)
        {
            var name = Path.GetFileNameWithoutExtension(fullPath);
            return name is "UnitTests" or "IntegrationTests" or "AcceptanceTests";
        }

        private static bool IsHost(XDocument document)
        {
            var sdk = document.Root?.Attribute("Sdk")?.Value ?? "";
            if (sdk.Contains("AppHost", StringComparison.OrdinalIgnoreCase)
                || sdk.Contains("Worker", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var outputElement = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "OutputType");
            var output = outputElement == null ? "" : outputElement.Value.Trim();
            return output.Equals("Exe", StringComparison.OrdinalIgnoreCase)
                || output.Equals("WinExe", StringComparison.OrdinalIgnoreCase);
        }
    }
}

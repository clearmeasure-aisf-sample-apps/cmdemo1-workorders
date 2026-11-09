using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Flags direct PackageReferences whose compile assemblies are absent from the built assembly.
/// Tooling packages with no compile assembly are not flagged. Community Qodana has no unused-NuGet inspection.
/// </summary>
public static class UnusedPackageGate
{
    public static bool RequireOutputs =>
        string.Equals(Environment.GetEnvironmentVariable("DEAD_CODE_GATE_REQUIRE_OUTPUTS"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Build configuration whose outputs the gate reads.
    /// </summary>
    public static string Configuration()
    {
        var build = Environment.GetEnvironmentVariable("BuildConfiguration");
        if (!string.IsNullOrWhiteSpace(build))
        {
            return build;
        }

        var ci = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION");
        if (!string.IsNullOrWhiteSpace(ci))
        {
            return ci;
        }

#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    /// <summary>
    /// Scans every project under <paramref name="repoRoot"/>/<c>src</c>.
    /// </summary>
    public static UnusedPackageScan ScanSolution(string repoRoot, string configuration)
    {
        var src = Path.Combine(repoRoot, "src");
        var missing = new List<string>();
        var unused = new List<UnusedPackage>();
        foreach (var project in ProjectFiles(src))
        {
            CollectProject(new ProjectInputs(repoRoot, configuration, project), missing, unused);
        }

        unused.Sort(ComparePackages);
        return new UnusedPackageScan(unused, missing);
    }

    /// <summary>
    /// Selects package ids whose assemblies are not in <paramref name="referencedAssemblies"/>.
    /// Packages with no compile or runtime assembly are treated as tooling and kept.
    /// </summary>
    public static IReadOnlyList<UnusedPackage> SelectUnused(
        string projectRelative,
        IEnumerable<string> packageIds,
        IReadOnlyDictionary<string, IReadOnlySet<string>> packageAssemblies,
        IReadOnlySet<string> referencedAssemblies)
    {
        var unused = new List<UnusedPackage>();
        foreach (var packageId in packageIds)
        {
            if (IsUnused(packageId, packageAssemblies, referencedAssemblies))
            {
                unused.Add(new UnusedPackage(Normalize(projectRelative), packageId));
            }
        }

        return unused;
    }

    /// <summary>
    /// Assembly names referenced by a managed assembly.
    /// </summary>
    public static HashSet<string> ReadAssemblyReferences(string assemblyPath)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata)
        {
            return names;
        }

        var reader = pe.GetMetadataReader();
        foreach (var handle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(handle);
            names.Add(reader.GetString(reference.Name));
        }

        return names;
    }

    /// <summary>
    /// Reads the ratchet list. Every entry needs a reason. The list may only shrink.
    /// </summary>
    public static IReadOnlyList<PackageBaselineEntry> ReadBaseline(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var entries = new List<PackageBaselineEntry>();
        foreach (var element in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            entries.Add(ReadEntry(element));
        }

        return entries;
    }

    /// <summary>
    /// Packages found that are not on the baseline. A smaller found set still passes.
    /// </summary>
    public static IReadOnlyList<UnusedPackage> NotInBaseline(
        IReadOnlyList<UnusedPackage> found,
        IReadOnlyList<PackageBaselineEntry> baseline)
    {
        var allowed = AllowedKeys(baseline);
        var unexpected = new List<UnusedPackage>();
        foreach (var package in found)
        {
            if (!allowed.Contains(Key(package.Project, package.PackageId)))
            {
                unexpected.Add(package);
            }
        }

        return unexpected;
    }

    private readonly record struct ProjectInputs(string RepoRoot, string Configuration, string Project);

    private static void CollectProject(ProjectInputs input, List<string> missing, List<UnusedPackage> unused)
    {
        var relative = Relative(input.RepoRoot, input.Project);
        var projectDirectory = Path.GetDirectoryName(input.Project)!;
        var assets = Path.Combine(projectDirectory, "obj", "project.assets.json");
        var output = ResolveOutputPath(input.Project, input.Configuration);
        if (!File.Exists(assets) || !File.Exists(output))
        {
            missing.Add(relative);
            return;
        }

        var referenced = ReadAssemblyReferences(output);
        var assemblies = PackageAssemblies(assets);
        var tooling = ToolingPackages(assets);
        var ids = ReadPackageIds(XDocument.Load(input.Project));
        var source = ReadProjectSource(projectDirectory);
        foreach (var package in SelectUnused(relative, ids, assemblies, referenced))
        {
            if (tooling.Contains(package.PackageId) || SourceMentions(source, package.PackageId, assemblies))
            {
                continue;
            }

            unused.Add(package);
        }
    }

    private static string ReadProjectSource(string projectDir)
    {
        var builder = new StringBuilder();
        foreach (var file in Directory.EnumerateFiles(projectDir, "*", SearchOption.AllDirectories))
        {
            if (!IsProjectSource(file))
            {
                continue;
            }

            builder.Append(File.ReadAllText(file));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static bool IsProjectSource(string path)
    {
        if (IsUnderSkippedDirectory(path))
        {
            return false;
        }

        return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SourceMentions(
        string source,
        string packageId,
        IReadOnlyDictionary<string, IReadOnlySet<string>> assemblies)
    {
        if (source.Contains(packageId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TryGetAssemblies(packageId, assemblies, out var names))
        {
            return false;
        }

        foreach (var name in names)
        {
            if (source.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static PackageBaselineEntry ReadEntry(JsonElement element)
    {
        var project = element.GetProperty("project").GetString() ?? "";
        var package = element.GetProperty("package").GetString() ?? "";
        var reason = element.GetProperty("reason").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException($"Unused package baseline entry {project} {package} has no reason.");
        }

        return new PackageBaselineEntry(Normalize(project), package, reason);
    }

    private static HashSet<string> AllowedKeys(IReadOnlyList<PackageBaselineEntry> baseline)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in baseline)
        {
            if (string.IsNullOrWhiteSpace(entry.Reason))
            {
                throw new InvalidOperationException(
                    $"Unused package baseline entry {entry.Project} {entry.PackageId} has no reason.");
            }

            allowed.Add(Key(entry.Project, entry.PackageId));
        }

        return allowed;
    }

    private static bool IsUnused(
        string packageId,
        IReadOnlyDictionary<string, IReadOnlySet<string>> packageAssemblies,
        IReadOnlySet<string> referencedAssemblies)
    {
        if (!TryGetAssemblies(packageId, packageAssemblies, out var assemblies))
        {
            return false;
        }

        foreach (var assembly in assemblies)
        {
            if (referencedAssemblies.Contains(assembly))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetAssemblies(
        string packageId,
        IReadOnlyDictionary<string, IReadOnlySet<string>> packageAssemblies,
        out IReadOnlySet<string> assemblies)
    {
        foreach (var pair in packageAssemblies)
        {
            if (string.Equals(pair.Key, packageId, StringComparison.OrdinalIgnoreCase) && pair.Value.Count > 0)
            {
                assemblies = pair.Value;
                return true;
            }
        }

        assemblies = new HashSet<string>();
        return false;
    }

    private static List<string> ProjectFiles(string srcRoot)
    {
        var files = new List<string>();
        foreach (var path in Directory.EnumerateFiles(srcRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (!IsUnderSkippedDirectory(path))
            {
                files.Add(path);
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private static bool IsUnderSkippedDirectory(string path)
    {
        foreach (var segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment is "bin" or "obj")
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ReadPackageIds(XDocument document)
    {
        var ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var ids = new List<string>();
        foreach (var element in document.Descendants(ns + "PackageReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(include))
            {
                ids.Add(include);
            }
        }

        return ids;
    }

    private static string ResolveOutputPath(string projectPath, string configuration)
    {
        var document = XDocument.Load(projectPath);
        var directory = Path.GetDirectoryName(projectPath)!;
        var assembly = AssemblyName(document, projectPath);
        var tfm = Property(document, "TargetFramework");
        if (string.IsNullOrWhiteSpace(tfm))
        {
            tfm = "net10.0";
        }

        return Path.Combine(directory, "bin", configuration, tfm, assembly + ".dll");
    }

    private static string AssemblyName(XDocument document, string projectPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var raw = Property(document, "AssemblyName");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return projectName;
        }

        var resolved = raw.Replace("$(MSBuildProjectName)", projectName, StringComparison.OrdinalIgnoreCase);
        return resolved.Contains("$(") ? projectName : resolved;
    }

    private static string? Property(XDocument document, string name)
    {
        var ns = document.Root?.Name.Namespace ?? XNamespace.None;
        foreach (var element in document.Descendants(ns + name))
        {
            return element.Value.Trim();
        }

        return null;
    }

    private static Dictionary<string, IReadOnlySet<string>> PackageAssemblies(string assetsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (!document.RootElement.TryGetProperty("targets", out var targets))
        {
            return ToReadOnly(map);
        }

        foreach (var tfm in targets.EnumerateObject())
        {
            AddTargetAssemblies(map, tfm.Value);
        }

        return ToReadOnly(map);
    }

    private static void AddTargetAssemblies(Dictionary<string, HashSet<string>> map, JsonElement packages)
    {
        foreach (var package in packages.EnumerateObject())
        {
            var id = PackageId(package.Name);
            if (!map.TryGetValue(id, out var names))
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[id] = names;
            }

            AddAssemblyNames(names, package.Value);
        }
    }

    private static void AddAssemblyNames(HashSet<string> names, JsonElement package)
    {
        foreach (var kind in new[] { "compile", "runtime" })
        {
            if (!package.TryGetProperty(kind, out var group) || group.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var file in group.EnumerateObject())
            {
                AddAssemblyName(names, file.Name);
            }
        }
    }

    private static void AddAssemblyName(HashSet<string> names, string assetPath)
    {
        var file = assetPath.Replace('\\', '/');
        var name = Path.GetFileNameWithoutExtension(file);
        if (name.Length == 0 || name == "_" || name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        names.Add(name);
    }

    private static HashSet<string> ToolingPackages(string assetsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var tooling = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!document.RootElement.TryGetProperty("targets", out var targets))
        {
            return tooling;
        }

        foreach (var tfm in targets.EnumerateObject())
        {
            AddToolingPackages(tooling, tfm.Value);
        }

        return tooling;
    }

    private static void AddToolingPackages(HashSet<string> tooling, JsonElement packages)
    {
        foreach (var package in packages.EnumerateObject())
        {
            if (HasToolingAssets(package.Value))
            {
                tooling.Add(PackageId(package.Name));
            }
        }
    }

    private static bool HasToolingAssets(JsonElement package)
    {
        foreach (var kind in new[] { "build", "buildMultiTargeting", "native" })
        {
            if (!package.TryGetProperty(kind, out var group) || group.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var asset in group.EnumerateObject())
            {
                var name = Path.GetFileName(asset.Name.Replace('\\', '/'));
                if (!name.Equals("_._", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Dictionary<string, IReadOnlySet<string>> ToReadOnly(Dictionary<string, HashSet<string>> map)
    {
        var readOnly = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in map)
        {
            readOnly[pair.Key] = pair.Value;
        }

        return readOnly;
    }

    private static string PackageId(string key)
    {
        var slash = key.IndexOf('/');
        return slash < 0 ? key : key[..slash];
    }

    private static string Relative(string root, string path)
    {
        return Normalize(Path.GetRelativePath(root, path));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string Key(string project, string packageId) => Normalize(project) + "|" + packageId;

    private static int ComparePackages(UnusedPackage left, UnusedPackage right)
    {
        var project = string.Compare(left.Project, right.Project, StringComparison.OrdinalIgnoreCase);
        return project != 0
            ? project
            : string.Compare(left.PackageId, right.PackageId, StringComparison.OrdinalIgnoreCase);
    }
}

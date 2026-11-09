namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

internal static class StructureFixture
{
    public const string LibraryProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    public const string ExeProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType>
          </PropertyGroup>
        </Project>
        """;

    private static string CreateRepo()
    {
        var path = Path.Combine(Path.GetTempPath(), "structure-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Write(string repo, string relativePath, string content)
    {
        var full = Path.Combine(repo, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(full, content);
    }

    private static void Delete(string repo)
    {
        if (Directory.Exists(repo))
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    public static StructureReport ScanDemo(string source)
    {
        return Scan(repo =>
        {
            Write(repo, "src/Demo/Demo.csproj", LibraryProject);
            Write(repo, "src/Demo/Sample.cs", source);
        });
    }

    public static StructureReport Scan(Action<string> arrange)
    {
        var repo = CreateRepo();
        try
        {
            arrange(repo);
            return SuperfluousStructureScanner.Scan(repo);
        }
        finally
        {
            Delete(repo);
        }
    }

    public static string EmptyAllowList()
    {
        var rules = StructureRule.All.Select(rule => $"\"{rule}\": []");
        return "{\n" + string.Join(",\n", rules) + "\n}";
    }

    public static string Baseline(string rule, int max, params string[] ids)
    {
        var lines = new List<string>();
        foreach (var name in StructureRule.All)
        {
            if (name == rule)
            {
                var quoted = string.Join(", ", ids.Select(id => $"\"{id}\""));
                lines.Add($"\"{name}\": {{\"max\": {max}, \"ids\": [{quoted}]}}");
            }
            else
            {
                lines.Add($"\"{name}\": {{\"max\": 0, \"ids\": []}}");
            }
        }

        return "{\n" + string.Join(",\n", lines) + "\n}";
    }
}

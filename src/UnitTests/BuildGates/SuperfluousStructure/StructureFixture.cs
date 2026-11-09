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

    public static StructureReport ScanDemo(string source)
    {
        return Scan(repo => repo
            .Write("src/Demo/Demo.csproj", LibraryProject)
            .Write("src/Demo/Sample.cs", source));
    }

    public static StructureReport Scan(Action<ScratchTree> arrange)
    {
        using var repo = new ScratchTree("structure-gate");
        arrange(repo);
        return SuperfluousStructureScanner.Scan(repo.Checkout);
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

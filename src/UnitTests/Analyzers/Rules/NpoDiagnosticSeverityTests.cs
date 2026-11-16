using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class NpoDiagnosticSeverityTests
{
    [Test]
    public void ShouldTreatCm0001AndCm0005AsErrors_And_Cm0002AsWarning()
    {
        var text = File.ReadAllText(EditorConfigPath());

        text.ShouldContain("dotnet_diagnostic.CM0001.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0005.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0002.severity = warning");
        text.ShouldContain("dotnet_diagnostic.CM0003.severity = none");
        text.ShouldContain("dotnet_diagnostic.CM0004.severity = none");
    }

    private static string EditorConfigPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var solution = Path.Combine(dir.FullName, "src", "ChurchBulletin.sln");
            if (File.Exists(solution))
            {
                return Path.Combine(dir.FullName, "src", ".editorconfig");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate src/.editorconfig");
    }
}
